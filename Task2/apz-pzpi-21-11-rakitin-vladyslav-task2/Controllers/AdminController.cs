using apz_backend.Data;
using apz_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.Design;
using System.Globalization;

namespace apz_backend.Controllers
{
    [Route("api/admin/")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public AdminController(ApiDbContext context) => _context = context;

        // Метод для отримання всіх військових частин
        [HttpGet("units/get_all")]
        public async Task<IActionResult> GetAllUnits()
        {
            var units = await _context.Units.ToListAsync();

            return Ok(units);
        }

        // Метод для отримання військової частини за ідентифікатором
        [HttpGet("units/get_by_id")]
        public async Task<IActionResult> GetUnitById(int id)
        {
            var unit = await _context.Units.FindAsync(id);

            return unit == null ? NotFound() : Ok(unit);
        }

        // Метод для додавання нової військової частини
        [HttpPost("units/add")]
        public async Task<IActionResult> AddUnit(int number, string type, string? name, string location, string? flag)
        {
            if (await _context.Units
                .AnyAsync(u => u.Number == number && u.Type == type && u.Name == name)) return Conflict();

            Unit unit = new Unit
            {
                Number = number,
                Type = type,
                Name = name,
                Location = location,
                Flag = flag
            };

            await _context.Units.AddAsync(unit);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUnitById), new { id = unit.Id }, unit);
        }

        // Метод для редагування військової частини
        [HttpPut("units/edit")]
        public async Task<IActionResult> EditUnit(int id, int number, string type, string? name, string location, string? flag)
        {
            var unit = await _context.Units.FindAsync(id);

            if (unit == null)
            {
                return NotFound();
            }

            if (await _context.Units.AnyAsync(u => u.Id != id && u.Number == number && u.Type == type && u.Name == name))
            {
                return Conflict();
            }

            unit.Number = number;
            unit.Type = type; 
            unit.Name = name;
            unit.Location = location;
            unit.Flag = flag;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для видалення військової частини
        [HttpDelete("units/delete")]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            var unitToDelete = await _context.Units.FindAsync(id);
            if (unitToDelete == null) return NotFound();
            _context.Units.Remove(unitToDelete);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання всіх командирів
        [HttpGet("commanders/get_all")]
        public async Task<IActionResult> GetAllCommanders()
        {
            var commanders = await _context.Commanders.ToListAsync();

            return Ok(commanders);
        }

        // Метод для отримання командира за ідентифікатором
        [HttpGet("commanders/get_by_id")]
        public async Task<IActionResult> GetCommanderById(int id)
        {
            var commander = await _context.Commanders.FindAsync(id);

            return commander == null ? NotFound() : Ok(commander);
        }

        // Метод для отримання всіх командирів певної військової частини
        [HttpGet("commanders/get_all_by_unit_id")]
        public async Task<IActionResult> GetCommandersByUnitId(int unitId)
        {
            var unit = await _context.Units.FindAsync(unitId);

            if (unit == null)
            {
                return NotFound();
            }

            var commanders = await _context.Units.Where(u => u.Id == unitId)
            .SelectMany(u => u.Commanders).AsNoTracking().ToListAsync();

            return Ok(commanders);
        }

        // Метод для додавання нового командира
        [HttpPost("commanders/add")]
        public async Task<IActionResult> AddCommander(string userName, string userPassword, string fullName,
            DateTime dateOfBirth, string rank, string? photoURL, int unitId)
        {
            if (await _context.Commanders
                .AnyAsync(c => c.UserName == userName)) return Conflict();

            if (dateOfBirth > DateTime.Now.AddYears(-18)) return BadRequest();

            Commander commander = new Commander
            {
                UserName = userName,
                UserPassword = userPassword,
                FullName = fullName,
                DateOfBirth = dateOfBirth,
                Rank = rank,
                PhotoURL = photoURL,
                UnitId = unitId
            };

            await _context.Commanders.AddAsync(commander);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCommanderById), new { id = commander.Id }, commander);
        }

        // Метод для редагування командира
        [HttpPut("commanders/edit")]
        public async Task<IActionResult> EditCommander(int id, string userName, string userPassword,
            string fullName, DateTime dateOfBirth, string rank, string? photoURL, int unitId)
        {
            var commander = await _context.Commanders.FindAsync(id);
            var unit = await _context.Units.FindAsync(unitId);

            if (commander == null || unit == null)
            {
                return NotFound();
            }

            if (await _context.Commanders.AnyAsync(c => c.Id != id && c.UserName == userName))
            {
                return Conflict();
            }

            if (dateOfBirth > DateTime.Now.AddYears(-18)) return BadRequest();

            commander.UserName = userName;
            commander.UserPassword = userPassword;
            commander.FullName = fullName;
            commander.DateOfBirth = dateOfBirth;
            commander.Rank = rank;
            commander.PhotoURL = photoURL;
            commander.UnitId = unitId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для видалення командира
        [HttpDelete("commanders/delete")]
        public async Task<IActionResult> DeleteCommander(int id)
        {
            var commanderToDelete = await _context.Commanders.FindAsync(id);
            if (commanderToDelete == null) return NotFound();
            _context.Commanders.Remove(commanderToDelete);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для отримання всіх солдатів
        [HttpGet("soldiers/get_all")]
        public async Task<IActionResult> GetAllSoldiers()
        {
            var soldiers = await _context.Soldiers.ToListAsync();

            return Ok(soldiers);
        }

        // Метод для отримання солдата за ідентифікатором
        [HttpGet("soldiers/get_by_id")]
        public async Task<IActionResult> GetSoldierById(int id)
        {
            var soldier = await _context.Soldiers.FindAsync(id);

            return soldier == null ? NotFound() : Ok(soldier);
        }

        // Метод для отримання всіх солдатів за ідентифікатором командира
        [HttpGet("soldiers/get_all_by_commander_id")]
        public async Task<IActionResult> GetSoldiersByCommanderId(int commanderId)
        {
            var commander = await _context.Commanders.FindAsync(commanderId);

            if (commander == null)
            {
                return NotFound();
            }

            var soldiers = await _context.Commanders.Where(c => c.Id == commanderId)
            .SelectMany(c => c.Soldiers).AsNoTracking().ToListAsync();

            return Ok(soldiers);
        }

        // Метод для отримання всіх солдатів за ідентифікатором військової частини
        [HttpGet("soldiers/get_all_by_unit_id")]
        public async Task<IActionResult> GetSoldiersByUnitId(int unitId)
        {
            var unit = await _context.Units.FindAsync(unitId);

            if (unit == null)
            {
                return NotFound();
            }

            var soldiers = await _context.Units.Where(u => u.Id == unitId)
            .SelectMany(u => u.Commanders).SelectMany(s => s.Soldiers).AsNoTracking().ToListAsync();

            return Ok(soldiers);
        }
        // Метод для додавання нового солдата
        [HttpPost("soldiers/add")]
        public async Task<IActionResult> AddSoldier(string userName, string userPassword, string fullName,
            DateTime dateOfBirth, string rank, string? photoURL, DateTime enlistDate, DateTime? dischargeDate, int commanderId)
        {
            if (await _context.Soldiers
                .AnyAsync(s => s.UserName == userName)) return Conflict();

            if (dateOfBirth > DateTime.Now.AddYears(-18) || enlistDate > dischargeDate) return BadRequest();

            Soldier soldier = new Soldier
            {
                UserName = userName,
                UserPassword = userPassword,
                FullName = fullName,
                DateOfBirth = dateOfBirth,
                Rank = rank,
                PhotoURL = photoURL,
                EnlistDate = enlistDate,
                DischargeDate = dischargeDate,
                CommanderId = commanderId
            };

            await _context.Soldiers.AddAsync(soldier);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSoldierById), new { id = soldier.Id }, soldier);
        }

        // Метод для редагування інформації про солдата
        [HttpPut("soldiers/edit")]
        public async Task<IActionResult> EditSoldier(int id, string userName, string userPassword, string fullName,
            DateTime dateOfBirth, string rank, string? photoURL, DateTime enlistDate, DateTime dischargeDate, int commanderId)
        {
            var soldier = await _context.Soldiers.FindAsync(id);
            var commander = await _context.Commanders.FindAsync(commanderId);

            if (soldier == null || commander == null)
            {
                return NotFound();
            }

            if (await _context.Soldiers.AnyAsync(s => s.Id != id && s.UserName == userName))
            {
                return Conflict();
            }

            if (dateOfBirth > DateTime.Now.AddYears(-18) || enlistDate > dischargeDate) return BadRequest();

            soldier.UserName = userName;
            soldier.UserPassword = userPassword;
            soldier.FullName = fullName;
            soldier.DateOfBirth = dateOfBirth;
            soldier.Rank = rank;
            soldier.PhotoURL = photoURL;
            soldier.EnlistDate = enlistDate;
            soldier.DischargeDate = dischargeDate;
            soldier.CommanderId = commanderId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Метод для видалення солдата
        [HttpDelete("soldiers/delete")]
        public async Task<IActionResult> DeleteSoldier(int id)
        {
            var soldierToDelete = await _context.Soldiers.FindAsync(id);
            if (soldierToDelete == null) return NotFound();
            _context.Soldiers.Remove(soldierToDelete);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}