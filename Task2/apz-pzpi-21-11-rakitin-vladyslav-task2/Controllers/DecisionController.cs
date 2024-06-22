using apz_backend.Data;
using apz_backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;

namespace apz_backend.Controllers
{
    [Route("api/decision")]
    [ApiController]
    public class DecisionController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private VoteStorage _voteStorage;
        public DecisionController(ApiDbContext context, VoteStorage voteStorage)
        {
            _context = context;
            _voteStorage = voteStorage;
        }

        // метод пошуку військового для ротації за параметрами важливості критеріїв
        [HttpGet("soldier_to_rotate")]
        public async Task<IActionResult> FindSoldierToRotate(double k1 = 1, double k2 = 1, double k3 = 1, double k4 = 1)
        {
            var soldiers = await _context.Soldiers.ToListAsync();

            // матриця парних порівнянь
            double[,] pairedComparisonsArray = new double[soldiers.Count, soldiers.Count];

            // ігра «кожний з кожним»
            for (int i = 0; i < pairedComparisonsArray.GetLength(0); i++)
            {
                for (int k = 0; k < pairedComparisonsArray.GetLength(1); k++)
                {
                    // 0 - рівні, >0 - A(i) краща, <0 - A(i) гірша
                    double comparisonResult = 0;
                    comparisonResult += CompareByAge(soldiers[i], soldiers[k]) * k1 +
                        CompareBySleepsQuality(soldiers[i], soldiers[k]) * k2 +
                        CompareByRequestsAmount(soldiers[i], soldiers[k]) * k3 +
                        CompareByRotationsAmount(soldiers[i], soldiers[k]) * k4;
                    pairedComparisonsArray[i, k] = comparisonResult;
                }
            }

            // результати підрахунку за принципом «менше»
            int[] comparisonsCountArray = new int[soldiers.Count];

            for (int i = 0; i < comparisonsCountArray.Length; i++)
            {
                // лічильник кількості порівнянь, у яких альтернатива виявилася гіршою за іншу
                int counter = 0;

                for (int k = 0; k < pairedComparisonsArray.GetLength(0); k++)
                {
                    if (pairedComparisonsArray[i, k] < 0) counter++;
                }

                comparisonsCountArray[i] = counter;
            }

            // краща альтернатива - найменше разів гірша за іншу альтернативу
            int bestIndex = comparisonsCountArray.Min();

            return Ok(soldiers[bestIndex]);
        }

        // метод додавання голосу
        [HttpPost("soldiers_to_vote")]
        public async Task<IActionResult> AddSoldiersToVote(int soldier1Id, int soldier2Id, int soldier3Id)
        {
            bool allExist = await _context.Soldiers.AnyAsync(s => s.Id == soldier1Id) &&
                await _context.Soldiers.AnyAsync(s => s.Id == soldier2Id) &&
                await _context.Soldiers.AnyAsync(s => s.Id == soldier3Id);
            bool allUnique = soldier1Id != soldier2Id && soldier1Id != soldier3Id && soldier2Id != soldier3Id;
            // некоректний голос
            if (!allExist || !allUnique) return BadRequest();

            // додавання голосу
            int[] vote = new int[3] { soldier1Id, soldier2Id, soldier3Id };
            _voteStorage.voteList.Add(vote);

            return Ok();
        }

        // метод проведення голосування
        [HttpGet("collectively_voted_soldier")]
        public async Task<IActionResult> GetCollectivelyVotedSoldier()
        {
            var soldiers = await _context.Soldiers.ToListAsync();

            // результати підрахунку в першому турі
            int[] firstVoteArray = new int[soldiers.Count];

            // підрахунок кількості голосів для кожного кандидата
            for (int i = 0; i < firstVoteArray.Length; i++)
            {
                // лічильник кількості голосів, у яких альтернатива зайняла 1 місце
                int counter = 0;

                for (int k = 0; k < _voteStorage.voteList.Count; k++)
                {
                    if (_voteStorage.voteList[k][0] == soldiers[i].Id) counter++;
                }

                firstVoteArray[i] = counter;
            }

            int firstVoteMaxVotes = firstVoteArray.Max();
            int firstVoteFirstWinnerIndex =-1;
            int firstVoteSecondWinnerIndex = -1;

            // пошук переможців за кількістю голосів
            for (int i = 0; i < firstVoteArray.Length; i++)
            {
                if (firstVoteArray[i] == firstVoteMaxVotes)
                {
                    if (firstVoteFirstWinnerIndex == -1) firstVoteFirstWinnerIndex = i;
                    else firstVoteSecondWinnerIndex = i;
                }
            }

            // якщо кандидат набирає строгу більшість голосів - він перемагає
            if (firstVoteSecondWinnerIndex == -1) return Ok(soldiers[firstVoteFirstWinnerIndex]);

            else
            {
                // лічильник різниці голосів, які були віддані першому кандидату на противагу другому
                int counter = 0;

                // другий тур голосування з двома кандидатами
                for (int k = 0; k < _voteStorage.voteList.Count; k++)
                {
                    if (_voteStorage.voteList[k][0] == soldiers[firstVoteFirstWinnerIndex].Id) counter++;
                    else if (_voteStorage.voteList[k][0] == soldiers[firstVoteSecondWinnerIndex].Id) counter--;
                    else if (_voteStorage.voteList[k][1] == soldiers[firstVoteFirstWinnerIndex].Id) counter++;
                    else if (_voteStorage.voteList[k][1] == soldiers[firstVoteSecondWinnerIndex].Id) counter--;
                    else if (_voteStorage.voteList[k][2] == soldiers[firstVoteFirstWinnerIndex].Id) counter++;
                    else if (_voteStorage.voteList[k][2] == soldiers[firstVoteSecondWinnerIndex].Id) counter--;
                    else continue;
                }

                if (counter > 0) return Ok(soldiers[firstVoteFirstWinnerIndex]); // перший переміг другого - перший перемагає
                else if (counter < 0) return Ok(soldiers[firstVoteSecondWinnerIndex]); // другий програв першому - другий перемагає
                else return NotFound(); // кандидати рівні за результатами двох турів
            }
        }

        private int CompareByAge(Soldier s1, Soldier s2)
        {
            int ageS1 = DateTime.Now.Year - s1.DateOfBirth.Year;
            int ageS2 = DateTime.Now.Year - s2.DateOfBirth.Year;

            if (ageS1 > ageS2) return 1; // старший - більше потребує ротації
            else if (ageS1 < ageS2) return -1; // молодший - менше потребує ротації
            else return 0; // рівні за віком
        }

        private int CompareBySleepsQuality(Soldier s1, Soldier s2)
        {
            if (s1.Sleeps == null && s2.Sleeps == null)
                return 0; // немає інформації

            double? averageSleepQualityS1 = s1.Sleeps.Where(sl => sl.StartTime > DateTime.Now.AddDays(-7) &&
            sl.Quality != null).Select(sl => sl.Quality).Average();
            double? averageSleepQualityS2 = s2.Sleeps.Where(sl => sl.StartTime > DateTime.Now.AddDays(-7) &&
            sl.Quality != null).Select(sl => sl.Quality).Average();

            if (averageSleepQualityS1 != null && averageSleepQualityS2 != null)
            {
                if (averageSleepQualityS1 < averageSleepQualityS2)
                    return 1; // гірша якість сну - більше потребує ротації
                else if (averageSleepQualityS1 > averageSleepQualityS2)
                    return -1;// краща якість сну - менше потребує ротації
                else return 0; // рівні за якістю сну 
            }
            else return 0; // немає інформації
        }

        private int CompareByRequestsAmount(Soldier s1, Soldier s2)
        {
            if (s1.Requests == null && s2.Requests == null)
                return 0; // немає інформації

            int requestsAmountS1 = Math.Clamp(s1.Requests.Where(r => r.Time >
            DateTime.Now.AddMonths(-1)).Count(), 0, 5);
            int requestsAmountS2 = Math.Clamp(s2.Requests.Where(r => r.Time >
            DateTime.Now.AddMonths(-1)).Count(), 0, 5);

            if (requestsAmountS1 > requestsAmountS2) return 1; // більше запитів - більше потребує ротації
            else if (requestsAmountS1 > requestsAmountS2) return -1; // менше запитів - менше потребує ротації
            else return 0; // рівні за кількістю запитів
        }

        private int CompareByRotationsAmount(Soldier s1, Soldier s2)
        {
            if (s1.Rotations == null && s2.Rotations == null)
                return 0; // немає інформації

            int rotationsAmountS1 = Math.Clamp(s1.Rotations.Where(r => r.LeaveDate >
            DateTime.Now.AddYears(-1)).Count(), 0, 2);
            int rotationsAmountS2 = Math.Clamp(s2.Rotations.Where(r => r.LeaveDate >
            DateTime.Now.AddYears(-1)).Count(), 0, 2);

            if (rotationsAmountS1 < rotationsAmountS2) return 1; // менше запитів - більше потребує ротації
            else if (rotationsAmountS1 > rotationsAmountS2) return -1; // більше ротацій - менше потребує ротації
            else return 0; // рівні за кількістю ротацій
        }
    }
}
