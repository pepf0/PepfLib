namespace PepfLib.Grids;

public static class Dir
{
    public static readonly Cell[] Dir4 = [(-1, 0), (0, 1), (1, 0), (0, -1)];

    public static readonly Cell[] Dir8 = [(-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1)];

    public static readonly Cell[] DirHex = [(0, 2), (1, 1), (1, -1), (0, -2), (-1, -1), (-1, 1)];
}
