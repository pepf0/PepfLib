using System.Text;

namespace PepfLib.Grids;

public class Grid<T>
{
    // PROPERTIES

    // Inner array, storing the grid
    private T[,] Inner { get; }
    // Grid dimensions
    public Cell Size => (Inner.GetLength(0), Inner.GetLength(1));
    public int Rows => Size.Row;
    public int Cols => Size.Col;
    // Indexing through Cells or row/col ints
    public T this[Cell c] { get => Inner[c.Row, c.Col]; set => Inner[c.Row, c.Col] = value; }
    public T this[int r, int c] { get => Inner[r, c]; set => Inner[r, c] = value; }

    // CONSTRUCTORS

    // Create a grid from a T[,]
    public Grid(T[,] from)
        => Inner = from;

    // Create a rows x cols grid filled with one value
    public Grid(int rows, int cols, T fill)
    {
        Inner = new T[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                Inner[r, c] = fill;
    }

    // METHODS

    // Check if a Cell is inside the grid
    public bool InBounds(Cell c)
        => c.Row < Rows && c.Row >= 0 && c.Col < Cols && c.Col >= 0;

    // Get first Cell that meets the condition, going row by row
    public Cell? First(Predicate<T> p)
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (p(Inner[r, c])) return (r, c);
        return null;
    }

    // Get all Cells that meet the condition
    public IEnumerable<Cell> All(Predicate<T> p)
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (p(Inner[r, c])) yield return (r, c);
    }

    // Get all Cells with value t
    public IEnumerable<Cell> FindAll(T t) => All(x => EqualityComparer<T>.Default.Equals(x, t));

    // Get count of Cells that meet the condition
    public int Count(Predicate<T> p) => All(p).Count();

    // Get neighbors of a Cell that are inside the grid (Dir4 by default)
    public IEnumerable<Cell> Neighbors(Cell c, Cell[]? dirs = null)
    {
        dirs ??= Dir.Dir4;
        return dirs.Select(dir => c + dir).Where(InBounds);
    }
    // Distance of shortest path from start cell to end cell.
    public int ShortestDistance(Cell start, Cell end, Predicate<T> walkable, Cell[]? dirs = null)
        => ShortestDistance(this, start, end, walkable, dirs);
    // Shortest path from start cell to end cell
    public Cell[] ShortestPath(Cell start, Cell end, Predicate<T> walkable, Cell[]? dirs = null)
        => ShortestPath(this, start, end, walkable, dirs);
    // Shortest weighted path from start cell to end cell
    public int ShortestWeightedDistance(Grid<int> weight, Cell start, Cell end, Predicate<T> walkable, Cell[]? dirs = null)
        => ShortestWeightedDistance(this, weight, start, end, walkable, dirs);

    // Grid as a string, one line per row
    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++) sb.Append(Inner[r, c]);
            if (r < Rows - 1)
                sb.Append('\n');
        }
        return sb.ToString();
    }

    // Copy of the grid, changes to it don't affect the original
    public Grid<T> Clone() => new((T[,])Inner.Clone());

    // STATIC METHODS

    // Get best path distance from starting cell to end cell.
    public static int ShortestDistance(Grid<T> grid, Cell start, Cell end, Predicate<T> walkable, Cell[]? dirs = null)
    {
        if (start == end) return 0;
        var stepCount = new Grid<int>(grid.Rows, grid.Cols, -1);
        var next = new Queue<Cell>();
        next.Enqueue(start);
        stepCount[start] = 0;

        while (next.Count > 0)
        {
            var current = next.Dequeue();
            foreach (var nb in grid.Neighbors(current, dirs))
            {
                if (nb == end) return stepCount[current] + 1;
                if (walkable(grid[nb])
                    && stepCount[nb] == -1)
                {
                    stepCount[nb] = stepCount[current] + 1;
                    next.Enqueue(nb);
                }
            }
        }
        return -1;
    }
    // Get the best path from starting cell to end cell.
    public static Cell[] ShortestPath(Grid<T> grid, Cell start, Cell end, Predicate<T> walkable, Cell[]? dirs = null)
    {
        if (start == end) return [start];
        var prev = new Grid<Cell?>(grid.Rows, grid.Cols, null);
        var next = new Queue<Cell>();
        next.Enqueue(start);
        prev[start] = start;

        while (next.Count > 0)
        {
            var current = next.Dequeue();
            foreach (var nb in grid.Neighbors(current, dirs))
            {
                if (nb == end)
                {
                    prev[nb] = current;
                    return BackTracePath(nb, prev);
                }
                if (walkable(grid[nb])
                    && prev[nb] == null)
                {
                    prev[nb] = current;
                    next.Enqueue(nb);
                }
            }
        }
        return [];
    }
    // Each Cell saves the previous cell. Get a full path back.
    public static Cell[] BackTracePath(Cell end, Grid<Cell?> prev)
    {
        var current = end;
        List<Cell> path = new();

        while (prev[current] != current)
        {
            path.Add(current);
            current = prev[current] ?? throw new ArgumentException("Invalid Path!");
        }
        path.Add(current);
        path.Reverse();
        return path.ToArray();
    }
    // Get best weighted distance from starting cell to end cell, with each cell having a weight.
    public static int ShortestWeightedDistance(Grid<T> grid, Grid<int> weight, Cell start, Cell end,
        Predicate<T> walkable, Cell[]? dirs = null)
    {
        if (start == end) return 0;
        var weightDist = new Grid<int>(grid.Rows, grid.Cols, int.MaxValue);
        var next = new PriorityQueue<Cell, int>();
        weightDist[start] = 0;
        next.Enqueue(start, 0);

        while (next.TryDequeue(out var current, out var totalWeight))
        {
            if (totalWeight > weightDist[current]) continue;
            if (current == end) return totalWeight;

            foreach (var nb in grid.Neighbors(current, dirs))
            {
                if (!walkable(grid[nb])) continue;
                int nextWeight = totalWeight + weight[nb];
                if (nextWeight < weightDist[nb])
                {
                    weightDist[nb] = nextWeight;
                    next.Enqueue(nb, nextWeight);
                }
            }
        }
        return -1;
    }
}

// CELL RECORD STRUCT
public readonly record struct Cell(int Row, int Col)
{
    public static Cell operator +(Cell a, Cell b) => (a.Row + b.Row, a.Col + b.Col);
    public static Cell operator -(Cell a, Cell b) => (a.Row - b.Row, a.Col - b.Col);
    public static Cell operator -(Cell a) => (-a.Row, -a.Col);
    public static Cell operator *(Cell a, int b) => (a.Row * b, a.Col * b);
    public static Cell operator /(Cell a, int b) => (a.Row / b, a.Col / b);

    public static implicit operator Cell((int row, int col) a) => new Cell(a.row, a.col);
    public static implicit operator (int row, int col)(Cell a) => (a.Row, a.Col);
}
