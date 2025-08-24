using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    public class HittingModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingModel(AppDbContext context)
        {
            _context = context;
        }

        // Propiedad enlazada para el ID de la estadística seleccionada en el desplegable
        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        // Lista de tipos de estadística cargados desde la BD
        public List<StatType> StatTypes { get; set; } = new();
        // Lista de estadísticas de equipo que se mostrarán en la tabla
        public List<TeamStat> TeamStats { get; set; } = new();
        // Opciones para el desplegable de estadística, con formato SelectListItem
        public List<SelectListItem> StatOptions { get; set; } = new();
        // Diccionario que asocia el Id de StatType con su descripción
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar los tipos de estadística de la categoría Hitting
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Hitting")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // Rellenar las opciones del desplegable y añadir una opción "All"
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // Construir el diccionario de descripciones por Id
            StatDescriptions.Clear();
            foreach (var st in StatTypes)
            {
                string desc = st.Name.ToUpperInvariant() switch
                {
                    "AVG"  => "Batting average: hits divided by at-bats (H/AB)",
                    "OBP"  => "On-base percentage: (H + BB + HBP) / (AB + BB + HBP + SF)",
                    "SLG"  => "Slugging percentage: total bases per at-bat",
                    "OPS"  => "On-base plus slugging: OBP + SLG",
                    "R"    => "Runs scored: number of times a player safely reaches home plate.",
                    "H"    => "Hits: number of times a batter reaches base safely on a fair ball without an error or fielders choice",
                    "HR"   => "Home runs: hits where the batter circles all bases in one play, typically over the fence",
                    "RBI"  => "Runs batted in: runs scored as a result of the batters at-bat (excluding errors)",
                    "BB"   => "Walks: plate appearances resulting in four balls",
                    "SO"   => "Strikeouts: outs recorded on strike three",
                    "SB"   => "Stolen bases: bases taken successfully without the help of a hit or error",
                    "CS"   => "Caught stealing: runners thrown out while attempting to steal",
                    "2B"   => "Doubles: hits on which the batter reaches second base",
                    "3B"   => "Triples: hits on which the batter reaches third base",
                    "AB"   => "At-bats: plate appearances excluding walks, hit by pitch, sacrifices, and interference",
                    "AB/HR" => "At-bats per home run: AB divided by HR",
                    "GIDP" => "Ground into double play: when a player hits a ground ball that results in more than one out on the bases",
                    "HBP" => "Hit by pitch: when a batter is struck by a pitched ball without swinging",
                    "LOB" => "Left on base: number of runners left on base at the end of an inning",
                    "RLSP" => "Runners left in scoring position: Average number of runners who finish an inning on second or third base, without having scored, per game a team plays",
                    "S"   => "Singles: hits on which the batter reaches first base",
                    "SAC" => "Sacrifice hits: bunts that advance a runner while resulting in an out",
                    "SBA" => "Stolen base attempts: total number of times a player tries to steal a base, both successful and caught",
                    "SF"  => "Sacrifice flies: fly balls that allow a runner to score after the catch",
                    "TB"  => "Total bases: cumulative number of bases a player earns from hits (1 for single, 2 for double, etc.)",
                    "TLOB" => "Total left on base: total number of runners left on base at the end of each half-inning",
                    _      => ""
                    
                };
                StatDescriptions[st.Id.ToString()] = desc;
            }

            // Consulta base de estadísticas por equipo para la categoría Hitting
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Hitting")
                .AsQueryable();

            // Si hay un tipo seleccionado (distinto de All), filtrar por ese Id
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            // Ordenar y materializar resultados
            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }
    }
}
