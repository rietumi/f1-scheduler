using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

// Initialize test data
var incomeData = LoadJson<List<Dictionary<string, int?>>>("./test-data/income.json");
var travelData = LoadJson<Dictionary<string, Dictionary<string, int?>>>("./test-data/travel.json");
var stopwatch = new Stopwatch();
int weekends = 52;

while (true)
{
    stopwatch.Reset();
    var initialSolution = new string?[weekends];
    initialSolution[0] = "Bahrain";
    initialSolution[1] = "Belgium";
    initialSolution[2] = "Suzuka";
    initialSolution[3] = "Baku";
    initialSolution[4] = "Saudi Arabia";
    initialSolution[5] = "Australia";
    initialSolution[6] = "China";
    initialSolution[7] = "Miami";
    initialSolution[8] = "Emilia-Romagna";
    initialSolution[9] = "Monaco";
    initialSolution[10] = "Canada";
    initialSolution[11] = "Spain";
    initialSolution[12] = "Austria";
    initialSolution[13] = "Great Britain";
    initialSolution[14] = "Hungary";
    initialSolution[15] = "Netherlands";
    initialSolution[16] = "Italy";
    initialSolution[17] = "Singapore";
    initialSolution[18] = "Mexico";
    initialSolution[19] = "Brazil";
    initialSolution[20] = "Las Vegas";
    initialSolution[21] = "Qatar";
    initialSolution[22] = "Abu Dhabi";


    Console.WriteLine("Please provide maximum iteration count. (Skip with 'Enter', default of 1000 will be used)");
    if (!int.TryParse(Console.ReadLine(), out int maxIterations))
    {
        maxIterations = 1000;
    };

    Console.WriteLine("Please provide tabu list size. (Skip with 'Enter', default of 10 will be used)");
    if (!int.TryParse(Console.ReadLine(), out int tabuListSize))
    {
        tabuListSize = 10;
    };

    Console.WriteLine("Please provide comma separated list of ids for Grand Prix to include");
    for (int i = 0; i < 23; i++)
    {
        Console.WriteLine($"{i}: {initialSolution[i]}");
    }

    string? list = Console.ReadLine();

    if (list != null)
    {
        var includeList = list.Split(',');
        List<int> idList = new List<int>();
        foreach (var item in includeList)
        {
            if (int.TryParse(item, out int id))
            {
                idList.Add(id);
            }
        }

        if (idList.Any())
        {
            for (int i = 0; i < 23; i++)
            {
                if (!idList.Contains(i))
                {
                    initialSolution[i] = null;
                }
            }
        }
    }


    stopwatch.Start();
    var result = await Task.Run(() => TabuSearch(initialSolution, maxIterations, tabuListSize));
    stopwatch.Stop();
    Console.WriteLine("Best schedule found:");
    for (int i = 0; i < result.Length; i++)
    {
        Console.WriteLine($"Weekend {i}: {result[i] ?? "free"}");
    }

    Console.WriteLine($"Value: ${CalculateSchedulesValue(result)} milions");
    Console.WriteLine($"Search time: {stopwatch.ElapsedTicks} ms");
}


// Neighborhood function
List<string?[]> GetNeighbors(string?[] solution)
{
    List<string?[]> neighbors = new List<string?[]>();
    for (int i = 0; i < solution.Length; i++)
    {
        for (int j = i + 1; j < solution.Length; j++)
        {
            // Empty weekends doesn't impact anything.
            if (solution[j] == null && solution[i] == null) continue;

            var neighbor = new string?[weekends];
            solution.CopyTo(neighbor, 0);

            // Swaps weekends
            neighbor[i] = solution[j];
            neighbor[j] = solution[i];

            neighbors.Add(neighbor);
        }
    }
    return neighbors;
}

// Evaluation functions
int GetIncome(string grandPrix, int weekend)
{
#pragma warning disable CS8602 // Dereference of a possibly null reference. Let it crash if no test data was loaded.
    return incomeData[weekend]?[grandPrix] ?? 0;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}

int GetTravelCost(string grandPrix, string nextGrandPrix)
{
#pragma warning disable CS8602 // Dereference of a possibly null reference. Let it crash if no test data was loaded.
    return travelData[grandPrix]?[nextGrandPrix] ?? int.MaxValue;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
}

int CalculateSchedulesValue(string?[] solution)
{
    int result = 0;
    for (int i = 0; i < solution.Length; i++)
    {
        if (solution[i] == null) continue;

        result += GetIncome(solution[i]!, i);

        for (int j = i + 1; j < solution.Length; j++)
        {
            if (solution[j] != null)
            {
                result -= GetTravelCost(solution[i]!, solution[j]!);
                break;
            }
        }
    }
    return result;
}

// Tabu Search Algorithm
Task<string?[]> TabuSearch(string?[] initialSolution,
                    int maxIterations,
                    int tabuListSize)
{

    // Initialize tabu search
    string?[] bestSolution = initialSolution;
    string?[] currentSolution = initialSolution;
    List<string?[]> tabuList = new List<string?[]>();
    var comparer = new CompareSchedules();

    for (int iter = 0; iter < maxIterations; iter++)
    {
        List<string?[]> neighbors = GetNeighbors(currentSolution);
        string?[]? bestNeighbor = null;
        // We are interested in most profit.
        int bestNeighborFitness = -1;
        foreach (string?[] neighbor in neighbors)
        {
            if (!tabuList.Contains(neighbor, comparer))
            {
                int neighborFitness = CalculateSchedulesValue(neighbor);
                if (neighborFitness > bestNeighborFitness)
                {
                    bestNeighbor = neighbor;
                    bestNeighborFitness = neighborFitness;
                }
            }
        }
        if (bestNeighbor == null)
        {
            // No non-tabu neighbors found,
            // terminate the search
            break;
        }
        currentSolution = bestNeighbor;
        tabuList.Add(bestNeighbor);
        if (tabuList.Count > tabuListSize)
        {
            // Remove the oldest entry from the
            // tabu list if it exceeds the size
            tabuList.RemoveAt(0);
        }
        if (CalculateSchedulesValue(bestNeighbor) > CalculateSchedulesValue(bestSolution))
        {
            // Update the best solution if the
            // current neighbor is better
            bestSolution = bestNeighbor;
        }
    }
    return Task.FromResult(bestSolution);
}

static T? LoadJson<T>(string fileLocation)
{
    using (StreamReader sr = new StreamReader(fileLocation))
    {
        string json = sr.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json);
    }
}

class CompareSchedules : IEqualityComparer<string?[]>
{
    public bool Equals(string?[]? x, string?[]? y)
    {
        if (x == null || y == null) return false;
        if (x.Length != y.Length) return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i]) return false;
        }

        return true;
    }

    public int GetHashCode([DisallowNull] string?[] obj)
    {
        return 0;
    }
}
