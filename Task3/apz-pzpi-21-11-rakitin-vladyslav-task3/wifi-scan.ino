#include <WiFi.h>
#include <HTTPClient.h>

#define ENCODER_SW  13
#define ENCODER_CLK 14
#define ENCODER_DT  12
#define SENSOR 33

#define WIFI_SSID "Wokwi-GUEST"
#define WIFI_PASSWORD ""
#define WIFI_CHANNEL 6

const int intervalTime = 6000;
const String endpoint = "http://host.wokwi.internal:5000/api/soldier/sleeps/update";

int soldierId = -1;
int rotateCounter = 0;
int intervalCounter = 0;
bool isSleeping = false;
int sleepPhase = 0;
float previousTemperature = NULL;

void setup() {
  Serial.begin(115200);
  analogReadResolution(10);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  pinMode(ENCODER_CLK, INPUT);
  pinMode(ENCODER_DT, INPUT);
  pinMode(SENSOR, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCODER_CLK), onRotate, FALLING);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD, WIFI_CHANNEL);
  Serial.print("Встановлення сигналу...");
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.print("...");
  }
  Serial.println(" Підключено!");
  setSoldierId();
}

void loop() {
  intervalCounter++;
  int buttonState = digitalRead(ENCODER_SW);
  if (buttonState == LOW) {
    setSoldierId();
  }
  int sensorValue = analogRead(SENSOR);
  float sensorTemperature = 1 / (log(1 / (1023. / sensorValue - 1)) / 3950 + 1.0 / 298.15) - 273.15;
  float rotationAngle = rotateCounter * 18.0;
  if (intervalCounter >= intervalTime/1000) {
    intervalCounter = 0;
    Serial.println("-----------------");
    Serial.println("Контрольний вимір");
    if (previousTemperature != NULL) {
      Serial.print("Температура тіла: ");
      if (sensorTemperature > previousTemperature) {
        Serial.print("↑");
        Serial.print(sensorTemperature);
        Serial.print(" ℃ (на ");
        Serial.print(sensorTemperature - previousTemperature);
        Serial.print(" ℃ більше ніж ");
        Serial.print(intervalTime / 60000.0);
        Serial.println(" хвилин тому)");
      }
      else {
        Serial.print("↓");
        Serial.print(sensorTemperature);
        Serial.print(" ℃ (на ");
        Serial.print(abs(sensorTemperature - previousTemperature));
        Serial.print(" ℃ менше ніж ");
        Serial.print(intervalTime / 60000.0);
        Serial.println(" хвилин тому)");
      }
    }
    else {
      Serial.print("Температура тіла: ");
      Serial.print(sensorTemperature);
      Serial.println(" ℃ (немає попередніх даних)");
    }
    Serial.print("Кут нахила тіла: ");
    Serial.print(rotationAngle);
    if (rotationAngle >= 60.0 || rotationAngle <= -60.0) {
      Serial.println("° (відповідає лежачому положенню)");
    }
    else {
      Serial.println("° (відповідає стоячому положенню)");
    }
    Serial.print("Стан сну: ");
    if (isSleeping) {
      if (rotationAngle < 60.0 && rotationAngle > -60.0) {
        isSleeping = false;
        sleepPhase = 0;
        Serial.println("військовий прокинувся.");
        sendHTTP(false);
      }
      else {
        Serial.println("продовжується.");
      }
    }
    else {
      if (rotationAngle >= 60.0 || rotationAngle <= -60.0) {
        if (sensorTemperature <= previousTemperature || previousTemperature == NULL) {
          Serial.print(sleepPhase);
          Serial.print(" → ");
          sleepPhase++;
          Serial.print(sleepPhase);
          Serial.print(" фаза сну.");
          if (sleepPhase == 5) {
            isSleeping = true;
            Serial.println(" Військовий заснув.");
            sendHTTP(true);
          }
          else {
            Serial.println();
          }
        }
        else {
          Serial.print(sleepPhase);
          Serial.print(" ← ");
          sleepPhase = 0;
          Serial.print(sleepPhase);
          Serial.println(" фаза сну.");
        }
      }
      else {
        sleepPhase = 0;
        Serial.println("відсутній.");
      }
    }
    Serial.println("-----------------");
    previousTemperature = sensorTemperature;
  }
  else {
    Serial.print("t = ");
    Serial.print(sensorTemperature);
    Serial.print(" ℃, ");
    Serial.print("α = ");
    Serial.print(rotationAngle);
    Serial.println("°");
  }
  delay(1000);
}

void sendHTTP(bool start) {
  HTTPClient http;
  String url = endpoint + "?start=" + (start == true ? "true" : "false") +
    "&soldierId=" + soldierId;
  http.addHeader("Content-Type", "text/plain");
  http.begin(url);
  Serial.println("Відправка даних на сервер...");
  int httpResponseCode = http.PUT("Sent from IOT Device");
  if (httpResponseCode > 0) {
    if (httpResponseCode == 200) {
      Serial.println("Історію сну успішно оновлено!");
    }
    else if (httpResponseCode == 400) {
      Serial.println("Помилка! Поточний стан військового не змінено.");
    }
    else if (httpResponseCode == 404) {
      Serial.println("Помилка! Військового з встановленим ID не знайдено.");
      Serial.print("Оновіть дані. ");
      setSoldierId();
    }
    else {
      Serial.print("Необроблена помилка ");
      Serial.println(httpResponseCode);
    }
  }
  else {
    Serial.println("Помилка! Відсутній зв'язок з сервером!");
  }
  http.end();
}

void onRotate() {
  int direction = digitalRead(ENCODER_DT);
  if (direction == HIGH) {
    rotateCounter++;
  }
  if (direction == LOW) {
    rotateCounter--;
  }
}

void setSoldierId() {
  Serial.print("Введіть ID військового: ");
  Serial.read();
  while(!Serial.available()) {
    delay(1);
  }
  soldierId = Serial.parseInt() ;
  Serial.println(soldierId);
  Serial.println("ID військового успішно встановлено!");
}