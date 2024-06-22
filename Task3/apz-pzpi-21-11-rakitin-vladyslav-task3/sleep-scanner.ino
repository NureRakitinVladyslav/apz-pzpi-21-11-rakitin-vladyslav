// Підключення додаткових файлів для забезпечення доступу в інтернет
#include <WiFi.h>
#include <HTTPClient.h>

// Встановлення макросів для портів
#define ENCODER_SW  13
#define ENCODER_CLK 14
#define ENCODER_DT  12
#define SENSOR 33

// Встановлення макросів для підключення до мережі
#define WIFI_SSID "Wokwi-GUEST"
#define WIFI_PASSWORD ""
#define WIFI_CHANNEL 6

// Встановлення глобальних констант
const int intervalTime = 6000;
const String endpoint = "http://host.wokwi.internal:5000/api/soldier/sleeps/update";

// Встановлення глобальних змінних
int soldierId = -1;
int rotateCounter = 0;
int intervalCounter = 0;
bool isSleeping = false;
int sleepPhase = 0;
float previousTemperature = NULL;

// Налаштування пристрою перед циклом
void setup() {
  Serial.begin(115200);
  // Пов'язування пристроїв з кодом
  analogReadResolution(10);
  pinMode(ENCODER_SW, INPUT_PULLUP);
  pinMode(ENCODER_CLK, INPUT);
  pinMode(ENCODER_DT, INPUT);
  pinMode(SENSOR, INPUT);
  attachInterrupt(digitalPinToInterrupt(ENCODER_CLK), onRotate, FALLING);
  // Підключення до інтернету
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD, WIFI_CHANNEL);
  Serial.print("Встановлення сигналу...");
  while (WiFi.status() != WL_CONNECTED) {
    delay(1000);
    Serial.print("...");
  }
  Serial.println(" Підключено!");
  // Встановлення військового
  setSoldierId();
}

// Безперервний цикл пристрою
void loop() {
  intervalCounter++;
  // Перевірка кнопки ротатора на натиснутість
  int buttonState = digitalRead(ENCODER_SW);
  if (buttonState == LOW) {
    setSoldierId();
  }
  // Зчитування значення сенсора температури
  int sensorValue = analogRead(SENSOR);
  float sensorTemperature = 1 / (log(1 / (1023. / sensorValue - 1)) / 3950 + 1.0 / 298.15) - 273.15;
  float rotationAngle = rotateCounter * 18.0;
  // Контрольний вимір
  if (intervalCounter >= intervalTime/1000) {
    intervalCounter = 0;
    Serial.println("-----------------");
    Serial.println("Контрольний вимір");
    // Повторне зчитування
    if (previousTemperature != NULL) {
      Serial.print("Температура тіла: ");
      // Температура збільшилася
      if (sensorTemperature > previousTemperature) {
        Serial.print("↑");
        Serial.print(sensorTemperature);
        Serial.print(" ℃ (на ");
        Serial.print(sensorTemperature - previousTemperature);
        Serial.print(" ℃ більше ніж ");
        Serial.print(intervalTime / 60000.0);
        Serial.println(" хвилин тому)");
      }
      // Температура не збільшилася
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
    // Перше зчитування
    else {
      Serial.print("Температура тіла: ");
      Serial.print(sensorTemperature);
      Serial.println(" ℃ (немає попередніх даних)");
    }
    Serial.print("Кут нахила тіла: ");
    Serial.print(rotationAngle);
    // Військовий в лежачому положенні
    if (rotationAngle >= 60.0 || rotationAngle <= -60.0) {
      Serial.println("° (відповідає лежачому положенню)");
    }
    // Військовий в стоячому положенні
    else {
      Serial.println("° (відповідає стоячому положенню)");
    }
    Serial.print("Стан сну: ");
    // Якщо військовий вже спить
    if (isSleeping) {
      // Якщо військовий опинився в вертикальному положенні
      if (rotationAngle < 60.0 && rotationAngle > -60.0) {
        isSleeping = false;
        sleepPhase = 0;
        Serial.println("військовий прокинувся.");
        sendHTTP(false);
      }
      // Якщо військовий продовжив лежати
      else {
        Serial.println("продовжується.");
      }
    }
    // Якщо військовий ще не спить
    else {
      // Якщо військовий опинився в горизонтальному положенні
      if (rotationAngle >= 60.0 || rotationAngle <= -60.0) {
        // Якщо температура тіла зменшилася або залишилася незмінною
        if (sensorTemperature <= previousTemperature || previousTemperature == NULL) {
          Serial.print(sleepPhase);
          Serial.print(" → ");
          sleepPhase++;
          Serial.print(sleepPhase);
          Serial.print(" фаза сну.");
          // Якщо пройшов необхідний час для засинання
          if (sleepPhase == 5) {
            isSleeping = true;
            Serial.println(" Військовий заснув.");
            sendHTTP(true);
          }
          // Якщо ще не пройшов необхідний час для засинання
          else {
            Serial.println();
          }
        }
        // Якщо температура тіла збільшилася
        else {
          Serial.print(sleepPhase);
          Serial.print(" ← ");
          sleepPhase = 0;
          Serial.print(sleepPhase);
          Serial.println(" фаза сну.");
        }
      }
      // Якщо військовий продовжив стояти
      else {
        sleepPhase = 0;
        Serial.println("відсутній.");
      }
    }
    Serial.println("-----------------");
    previousTemperature = sensorTemperature;
  }
  // Звичайний вимір
  else {
    Serial.print("t = ");
    Serial.print(sensorTemperature);
    Serial.print(" ℃, ");
    Serial.print("α = ");
    Serial.print(rotationAngle);
    Serial.println("°");
  }
  // Затримка в 1 с перед наступним зчитуванням
  delay(1000);
}
// Метод відправки HTTP-запиту на сервер
void sendHTTP(bool start) {
  // Створення HTTP-клієнта
  HTTPClient http;
  String url = endpoint + "?start=" + (start == true ? "true" : "false") +
    "&soldierId=" + soldierId;
  http.addHeader("Content-Type", "text/plain");
  http.begin(url);
  Serial.println("Відправка даних на сервер...");
  // Відправка запиту на сервер
  int httpResponseCode = http.PUT("Sent from IOT Device");
  // Отримання відповіді
  if (httpResponseCode > 0) {
    // Успіх
    if (httpResponseCode == 200) {
      Serial.println("Історію сну успішно оновлено!");
    }
    // Некоректний запит
    else if (httpResponseCode == 400) {
      Serial.println("Помилка! Поточний стан військового не змінено.");
    }
    // Не знайдено
    else if (httpResponseCode == 404) {
      Serial.println("Помилка! Військового з встановленим ID не знайдено.");
      Serial.print("Оновіть дані. ");
      setSoldierId();
    }
    // Необроблена помилка
    else {
      Serial.print("Необроблена помилка ");
      Serial.println(httpResponseCode);
    }
  }
  // Відсутність відповіді
  else {
    Serial.println("Помилка! Відсутній зв'язок з сервером!");
  }
  // Завершення процедури
  http.end();
}
// Зміна куту нахилу військового
void onRotate() {
  // Зчитування напрямку нахилу
  int direction = digitalRead(ENCODER_DT);
  // Вправо
  if (direction == HIGH) {
    rotateCounter++;
  }
  // Вліво
  if (direction == LOW) {
    rotateCounter--;
  }
}

// Встановлення ID військового
void setSoldierId() {
  Serial.print("Введіть ID військового: ");
  Serial.read();
  while(!Serial.available()) {
    delay(1);
  }
  // Зчитування значення з клавіатури
  soldierId = Serial.parseInt() ;
  Serial.println(soldierId);
  Serial.println("ID військового успішно встановлено!");
}