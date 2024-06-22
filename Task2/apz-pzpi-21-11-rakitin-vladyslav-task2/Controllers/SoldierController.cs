using apz_backend.Data;
using apz_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace apz_backend.Controllers
{
    [Route("api/soldier")]
    [ApiController]
    public class SoldierController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public SoldierController(ApiDbContext context) => _context = context;

        // Метод для входу солдата
        [HttpGet("login")]
        public async Task<IActionResult> LoginSoldier(string userName, string userPassword)
        {
            var soldier = await _context.Soldiers.FirstOrDefaultAsync(u => u.UserName == userName);

            if (soldier == null)
            {
                return NotFound();
            }

            if (soldier.UserPassword != userPassword)
            {
                return Unauthorized();
            }

            return Ok(new { SoldierId = soldier.Id });
        }

        // Метод для зміни паролю солдата
        [HttpPut("change_password")]
        public async Task<IActionResult> ChangeSoldierPassword(int id, string userPassword)
        {
            var soldier = await _context.Soldiers.FindAsync(id);

            if (soldier == null)
            {
                return BadRequest();
            }

            soldier.UserPassword = userPassword;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання профілю солдата
        [HttpGet("get_profile")]
        public async Task<IActionResult> GetSoldierProfile(int id)
        {
            var soldier = await _context.Soldiers.Where(s => s.Id == id)
            .Include(s => s.Commander).ThenInclude(c => c.Unit).AsNoTracking().FirstAsync();

            if (soldier == null)
            {
                return BadRequest();
            }

            return Ok(soldier);
        }

        // Метод для отримання всіх спань за певний день для солдата за його ідентифікатором
        [HttpGet("sleeps/get_all_by_day")]
        public async Task<IActionResult> GetSleepsByDay(int soldierId, DateTime day)
        {
            var soldier = await _context.Soldiers.FindAsync(soldierId);

            if (soldier == null)
            {
                return BadRequest();
            }

            var sleeps = _context.Sleeps.Where(s => s.SoldierId == soldierId && (s.StartTime.Date == day.Date ||
                s.EndTime.Date == day.Date)).AsNoTracking().ToList();

            return Ok(sleeps);
        }

        // Метод для редагування якості спання
        [HttpPut("sleeps/edit/quality")]
        public async Task<IActionResult> EditSleepQuality(int sleepId, int quality)
        {
            var sleep = await _context.Sleeps.FindAsync(sleepId);

            if (sleep == null)
            {
                return BadRequest();
            }

            sleep.Quality = quality;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання всіх ротацій за ідентифікатором солдата
        [HttpGet("rotations/get_all_by_soldier_id")]
        public async Task<IActionResult> GetRotationsBySoldierId(int soldierId)
        {
            var soldier = await _context.Soldiers.FindAsync(soldierId);

            if (soldier == null)
            {
                return NotFound();
            }

            var rotations = await _context.Rotations.Where(r => r.SoldierId == soldierId).AsNoTracking().ToListAsync();

            return Ok(rotations);
        }

        // Метод для отримання запиту за ідентифікатором
        [HttpGet("requests/get_by_id")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var request = await _context.Requests.FindAsync(id);

            return request == null ? NotFound() : Ok(request);
        }

        // Метод для додавання нового запиту
        [HttpPost("requests/add")]
        public async Task<IActionResult> AddRequest(DateTime time, string reason, string? text, int soldierId)
        {
            Request request = new()
            {
                Time = time,
                Reason = reason,
                Text = text,
                SoldierId = soldierId
            };

            await _context.Requests.AddAsync(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRequestById), new { id = request.Id }, request);
        }

        // Метод для оновлення даних спання
        [HttpPut("sleeps/update")]
        public async Task<IActionResult> AddSleep(bool start, int soldierId)
        {
            //Перевірка ідентифікатору військового
            var soldier = await _context.Soldiers.FindAsync(soldierId);
            if (soldier == null) return NotFound();

            //Пошук останнього сна
            var sleeps = await _context.Sleeps.ToListAsync(); 
            Sleep? lastSleep;
            if (!sleeps.Any()) lastSleep = null;
            else lastSleep = sleeps.Where(s => s.SoldierId == soldierId).OrderBy(s => s.Id).Last();

            // Якщо сон починається
            if (start)
            {
                // Якщо останній сон ще не було завершено
                if (lastSleep != null && lastSleep.EndTime == DateTime.MinValue) return BadRequest();

                Sleep sleep = new()
                {
                    StartTime = DateTime.Now,
                    EndTime = DateTime.MinValue,
                    SoldierId = soldierId
                };

                await _context.Sleeps.AddAsync(sleep);
            }
            // Якщо сон закінчується
            else
            {
                // Якщо останній сон ще не було розпочато
                if (lastSleep == null || lastSleep.EndTime != DateTime.MinValue) return BadRequest();

                lastSleep.EndTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}