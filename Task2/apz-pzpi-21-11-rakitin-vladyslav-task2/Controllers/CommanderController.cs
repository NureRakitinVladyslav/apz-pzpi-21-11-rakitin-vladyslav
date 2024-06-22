using apz_backend.Data;
using apz_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace apz_backend.Controllers
{
    [Route("api/commander/")]
    [ApiController]
    public class CommanderController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public CommanderController(ApiDbContext context) => _context = context;

        // Метод для входу командира
        [HttpGet("login")]
        public async Task<IActionResult> LoginCommander(string userName, string userPassword)
        {
            var commander = await _context.Commanders.FirstOrDefaultAsync(u => u.UserName == userName);

            if (commander == null)
            {
                return NotFound();
            }

            if (commander.UserPassword != userPassword)
            {
                return Unauthorized();
            }

            return Ok(new { CommanderId = commander.Id });
        }

        // Метод для зміни паролю командира
        [HttpPut("change_password")]
        public async Task<IActionResult> ChangeCommanderPassword(int id, string userPassword)
        {
            var commander = await _context.Commanders.FindAsync(id);

            if (commander == null)
            {
                return BadRequest();
            }

            commander.UserPassword = userPassword;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання профілю командира
        [HttpGet("get_profile")]
        public async Task<IActionResult> GetCommanderProfile(int id)
        {
            var commander = await _context.Commanders.Where(c => c.Id == id)
            .Include(c => c.Unit).AsNoTracking().FirstOrDefaultAsync();

            if (commander == null)
            {
                return BadRequest();
            }

            return Ok(commander);
        }

        // Метод для отримання всіх солдатів за ідентифікатором 
        [HttpGet("soldiers/get_all_by_commander_id")]
        public async Task<IActionResult> GetSoldiersByCommanderId(int commanderId)
        {
            var commander = await _context.Commanders.FindAsync(commanderId);

            if (commander == null)
            {
                return BadRequest();
            }

            var soldiers = await _context.Soldiers.Where(s => s.CommanderId == commanderId)
            .Include
            (
                s => s.Sleeps.Where(sl => sl.StartTime > DateTime.Now.AddDays(-7))
            )
            .Include
            (
                s => s.Requests.Where(req => req.Time >
                (req.Soldier.Rotations == null ? DateTime.MinValue : req.Soldier.Rotations.Max(r => r.ReturnDate)))
            )
            .Include
            (
                s => s.Rotations.Where(rot => rot.LeaveDate > DateTime.Now.AddYears(-1))
            )
            .AsNoTracking().ToListAsync();

            return Ok(soldiers);
        }

        // Метод для редагування інформації про солдата
        [HttpPut("soldiers/edit")]
        public async Task<IActionResult> EditSoldier(int id, string rank)
        {
            var soldier = await _context.Soldiers.FindAsync(id);

            if (soldier == null)
            {
                return BadRequest();
            }

            soldier.Rank = rank;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання всіх спань за останній місяць для солдата за його ідентифікатором
        [HttpGet("sleeps/get_last_by_soldier_id")]
        public async Task<IActionResult> GetSleepsBySoldierId(int soldierId)
        {
            var soldier = await _context.Soldiers.FindAsync(soldierId);

            if (soldier == null)
            {
                return BadRequest();
            }

            var sleeps = await _context.Sleeps.Where(s => s.SoldierId == soldierId &&
                s.StartTime > DateTime.Now.AddMonths(-1)).AsNoTracking().ToListAsync();

            return Ok(sleeps);
        }

        // Метод для отримання всіх заявок за останню ротацію для солдата за його ідентифікатором
        [HttpGet("requests/get_last_by_soldier_id")]
        public async Task<IActionResult> GetRequestsBySoldierId(int soldierId)
        {
            var soldier = await _context.Soldiers.FindAsync(soldierId);

            if (soldier == null)
            {
                return BadRequest();
            }

            var requests = await _context.Requests.Where(r => r.SoldierId == soldierId && r.Time >
            (r.Soldier.Rotations == null ? DateTime.MinValue : r.Soldier.Rotations.Max(r => r.ReturnDate)))
            .AsNoTracking().ToListAsync();

            return Ok(requests);
        }

        // Метод для отримання ротації за ідентифікатором
        [HttpGet("rotations/get_by_id")]
        public async Task<IActionResult> GetRotationById(int id)
        {
            var rotation = await _context.Rotations.FindAsync(id);

            return rotation == null ? NotFound() : Ok(rotation);
        }

        // Метод для отримання останніх ротацій за останній рік для солдата за його ідентифікатором
        [HttpGet("rotations/get_last_by_soldier_id")]
        public async Task<IActionResult> GetRotationsBySoldierId(int soldierId)
        {
            var soldier = await _context.Soldiers.FindAsync(soldierId);

            if (soldier == null)
            {
                return BadRequest();
            }

            var rotations = await _context.Rotations.Where(r => r.SoldierId == soldierId &&
                r.LeaveDate > DateTime.Now.AddYears(-1)).AsNoTracking().ToListAsync();

            return Ok(rotations);
        }

        // Метод для додавання нової ротації
        [HttpPost("rotations/add")]
        public async Task<IActionResult> AddRotation(int days, string comment, int soldierId)
        {
            Rotation rotation = new()
            {
                LeaveDate = DateTime.Now.AddDays(1),
                ReturnDate = DateTime.Now.AddDays(1 + days + 1),
                Comment = comment,
                SoldierId = soldierId
            };

            await _context.Rotations.AddAsync(rotation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRotationById), new { id = rotation.Id }, rotation);
        }
    }
}