using DungeonSolver;

Setup[] setups =
    {
        new(new[] { 1, 4, 2, 7, 0, 4, 4, 4 }, new[] { 3, 2, 5, 3, 4, 1, 4, 4 }, new[] { (8, 2), (3, 3), (8, 4), (8, 6), (8, 8) }, new[] { (2, 6) }),
        new(new[] { 6, 2, 4, 1, 5, 4, 4, 5 }, new[] { 4, 4, 4, 4, 3, 4, 2, 6 }, new[] { (7, 1), (2, 5), (8, 6), (1, 8), (5, 8) }, new[] { (2, 3) }),
        new(new[] { 4, 2, 5, 0, 6, 2, 4, 2 }, new[] { 5, 2, 2, 1, 5, 3, 2, 5 }, new[] { (3, 3), (1, 7), (4, 8), (6, 8), (8, 8) }, new[] { (7, 3) }),
        new(new[] { 6, 2, 4, 3, 4, 4, 2, 6 }, new[] { 6, 2, 5, 3, 2, 5, 2, 6 }, new[] { (3, 1), (5, 1), (8, 3), (1, 4), (8, 5), (1, 6), (4, 8), (6, 8) }, new (int, int)[0]),
        new(new[] { 2, 4, 4, 3, 2, 3, 4, 2 }, new[] { 0, 7, 2, 4, 2, 2, 7, 0 }, new[] { (1, 1), (8, 1), (2, 3), (8, 3), (7, 6), (1, 8), (8, 8) }, new (int, int)[0]),
        new(new[] { 2, 3, 3, 4, 4, 3, 4, 6 }, new[] { 2, 3, 5, 4, 1, 4, 5, 5 }, new[] { (1, 1), (6, 2), (8, 2), (1, 5), (1, 8), (4, 8) }, new[] { (6, 5) })
    };

SolvePuzzle(new(setups[5]), true);

DeepNet net = new(256, new[] { 238 }, 64);
var iterations = 0;
var lastBadIteration = 0;
for (var wrong = 999; wrong > 0;)
{
    wrong = 0;
    foreach (var setup in setups.Take(3))
    {
        var result = ProcessSetup(net, setup, false);
        //Console.WriteLine(result);
        iterations += 8;
        if (result > 0)
        {
            lastBadIteration = iterations;
            wrong += result;
        }
        if (lastBadIteration < iterations - 17)
            break;
    }
}

Console.WriteLine(lastBadIteration);
var s = net.FeedForward(setups[3].ConvertToNeuralNetInput());

//Board b = new(setups[3]);
//for (var i = 0; i < 64; i++)
//    if (s[i] > 0)
//        b.State[(i & 0x7) + 1, (i >> 3) + 1] = CellState.Wall;
//b.Draw();


int ProcessSetup(DeepNet net, Setup s, bool showOutput)
{
    var result = RunNet(net, s, showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight().Mirror(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    result += RunNet(net, s = s.RotateRight(), showOutput);
    return result;
}

int RunNet(DeepNet net, Setup setup, bool showOutput)
{
    if (showOutput)
        Console.Clear();
    var board = SolvePuzzle(new(setup), showOutput);
    var result = board.GetWallsAsNeuralNetOutput();
    var input = setup.ConvertToNeuralNetInput();
    var wrong = TestSetup(net, input, result);

    if (showOutput)
        Console.WriteLine(wrong);

    return wrong;
}

Board SolvePuzzle(Board board, bool showOutput = false)
{
    if (showOutput)
        board.Draw();
    for (; board.SolutionState == SolutionState.None;)
    {
        if (CheckForCompletedLines(board, showOutput))
            continue;
        if (CheckForPathOut(board, showOutput))
            continue;
        if (CheckQuads(board, showOutput))
            continue;
        if (CheckForTreasureRoom(board, showOutput))
            continue;
        if (CheckForOneLeftInLine(board, showOutput))
            continue;
        // check for one left dead ends
        // if unk + empty = count + 1 -> all unks not in line next to monster and next to out of line wall are walls

        StartGuessing(ref board, showOutput);

        continue;
    }
    return board;
}

bool CheckForOneLeftInLine(Board board, bool showOutput)
{
    for (var i = 1; i <= 8; i++)
    {
        var unknowns = board.CountColumn(i, CellState.Unknown);
        var wallsRemaining = board.ColumnCounts[i - 1] - board.CountColumn(i, CellState.Wall);
        if (wallsRemaining == unknowns - 1) // the rest are walls except for one
            for (var j = 1; j <= 8; j++)
                if (board.State[i, j] == CellState.Unknown && (board.State[i - 1, j] == CellState.Wall || board.State[i + 1, j] == CellState.Wall)
                    && board.State[i, j - 1] != CellState.Monster && board.State[i, j + 1] != CellState.Monster)
                {
                    board.State[i, j] = CellState.Wall;
                    if (showOutput)
                    {
                        Console.WriteLine($"Cell ({i},{j}) must be a wall - it would create a dead end otherwise");
                        Redraw(board);
                    }
                        return true;
                }

        if (wallsRemaining == 1)
            for (var j = 1; j <= 8; j++)
                if (board.State[i, j] == CellState.Monster && board.State[i, j - 1] == CellState.Unknown && board.State[i, j + 1] == CellState.Unknown)
                {
                    board.State[i - 1, j] = CellState.Wall;
                    board.State[i + 1, j] = CellState.Wall;
                    for (var k = 1; k <= 8; k++)
                        if ((k < j - 1 || k > j + 1) && board.State[i, k] == CellState.Unknown)
                            board.State[i, k] = CellState.Empty;
                    if (showOutput)
                    { 
                        Console.WriteLine($"Monster at ({i}, {j}) must have Empty and Wall next to it, so the rest of column {i} is empty and it's next to walls in row {j}");
                        Redraw(board);
                    }
                    return false;
                }

        unknowns = board.CountRow(i, CellState.Unknown);
        wallsRemaining = board.RowCounts[i - 1] - board.CountRow(i, CellState.Wall);
        if (wallsRemaining == unknowns - 1) // the rest are walls except for one
            for (var j = 1; j <= 8; j++)
                if (board.State[j, i] == CellState.Unknown && (board.State[j, i - 1] == CellState.Wall || board.State[j, i + 1] == CellState.Wall)
                    && board.State[j - 1, i] != CellState.Monster && board.State[j + 1, i] != CellState.Monster)
                {
                    board.State[j, i] = CellState.Wall;
                    if (showOutput)
                    { 
                        Console.WriteLine($"Cell ({j},{i}) must be a wall - it would create a dead end otherwise");
                        Redraw(board);
                    }
                    return true;
                }

        if (wallsRemaining == 1)
            for (var j = 1; j <= 8; j++)
                if (board.State[j, i] == CellState.Monster && board.State[j - 1, i] == CellState.Unknown && board.State[j + 1, i] == CellState.Unknown)
                {
                    board.State[j, i - 1] = CellState.Wall;
                    board.State[j, i + 1] = CellState.Wall;
                    for (var k = 1; k <= 8; k++)
                        if ((k < j - 1 || k > j + 1) && board.State[k, i] == CellState.Unknown)
                            board.State[k, i] = CellState.Empty;
                    if (showOutput)
                    { 
                        Console.WriteLine($"Monster at ({j}, {i}) must have Empty and Wall next to it, so the rest of row {i} is empty and it's next to walls in column {j}");
                        Redraw(board);
                    }
                    return false;
                }
    }
    return false;
}

void StartGuessing(ref Board board, bool showOutput)
{
    for (var i = 1; i < 8; i++)
        for (var j = 1; j < 8; j++)
            if (board.State[i, j] == CellState.Unknown)
            {
                var testBoard = board.Clone();
                testBoard.State[i, j] = CellState.Wall;
                testBoard = SolvePuzzle(testBoard);
                if (testBoard.SolutionState == SolutionState.Solved)
                {
                    if (!showOutput)
                    {
                        board = testBoard;
                        return;
                    }
                    Console.WriteLine($"Hrmm, had to guess ({i},{j}) is a wall");
                    board.State[i, j] = CellState.Wall;
                }
                else
                {
                    if (showOutput)
                        Console.WriteLine($"Hrmm, ({i},{j}) can't be a wall");

                    board.State[i, j] = CellState.Empty;
                }
                if (showOutput)
                    Redraw(board);
                i = j = 8;
            }
}

int TestSetup(DeepNet net, float[] input, ulong solution)
{
    var solutionVector = new float[64];

    for (var i = 0; i < 64; i++)
        solutionVector[i] = ((solution >> (63 - i)) & 1) == 1 ? 1 : -1;

    var result = net.Train(input, solutionVector);
    var wrong = 0;
    for (var i = 0; i < 64; i++)
        if (MathF.Sign(result[i]) != MathF.Sign(solutionVector[i]))
            wrong++;
    return wrong;
}

static void Redraw(Board b)
{
    var cursorPosition = Console.GetCursorPosition();
    b.Draw();
    Console.SetCursorPosition(cursorPosition.Left, cursorPosition.Top);
}

static bool CheckForTreasureRoom(Board b, bool showOutput)
{
    var changed = false;

    for (var i = 1; i <= 8; i++)
        for (var j = 1; j <= 8; j++)
        {
            if (b.State[i, j] != CellState.Treasure)
                continue;
            var canBeRoom = b.CanBeRoom(i, j);
            if (canBeRoom.Count == 0)
                continue;
            if (canBeRoom.Count == 1)
            {
                var changedAny = false;
                for (var l = 0; l < 3; l++)
                    for (var m = 0; m < 3; m++)
                    {
                        var xx = canBeRoom[0].X + l;
                        var yy = canBeRoom[0].Y + m;
                        if (xx == i && yy == j)
                            continue;
                        if (b.State[xx, yy] != CellState.EmptyRoom)
                        {
                            b.State[xx, yy] = CellState.EmptyRoom;
                            changedAny = true;
                        }
                    }
                if (canBeRoom[0].HasExit)
                {
                    for (var l = 0; l < 3; l++)
                    {
                        if (b.State[canBeRoom[0].X + l, canBeRoom[0].Y - 1] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + l, canBeRoom[0].Y - 1] = CellState.Wall;
                            changedAny = true;
                        }
                        if (b.State[canBeRoom[0].X + l, canBeRoom[0].Y + 3] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + l, canBeRoom[0].Y + 3] = CellState.Wall;
                            changedAny = true;
                        }
                    }
                    for (var m = 0; m < 3; m++)
                    {
                        if (b.State[canBeRoom[0].X - 1, canBeRoom[0].Y + m] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X - 1, canBeRoom[0].Y + m] = CellState.Wall;
                            changedAny = true;
                        }
                        if (b.State[canBeRoom[0].X + 3, canBeRoom[0].Y + m] == CellState.Unknown)
                        {
                            b.State[canBeRoom[0].X + 3, canBeRoom[0].Y + m] = CellState.Wall;
                            changedAny = true;
                        }
                    }
                }
                if (changedAny)
                {
                    if (showOutput)
                    {
                        Console.WriteLine($"Found the room for treasure at ({i},{j})");
                        Redraw(b);
                    }
                    return true;
                }
            }
        }
    return false;
}

static bool CheckQuads(Board b, bool showOutput)
{
    for (var i = 1; i < 8; i++)
        for (var j = 1; j < 8; j++)
        {
            (int X, int Y)? unk = null;
            var fail = false;
            for (var k = 0; k < 4; k++)
            {
                var xp = i + (k >> 1);
                var yp = j + (k & 1);
                switch (b.State[xp, yp])
                {
                    case CellState.Unknown:
                        if (unk == null)
                            unk = (xp, yp);
                        else
                            fail = true;
                        break;
                    case CellState.Empty:
                        break;
                    default:
                        fail = true;
                        break;
                }
            }
            if (fail || unk == null)
                continue;
            b.State[unk.Value.X, unk.Value.Y] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Cell ({unk.Value.X},{unk.Value.Y}) must be a wall, paths can't be wider than 1");
                Redraw(b);
            }
            return true;
        }
    return false;
}

static bool CheckForCompletedLines(Board b, bool showOutput)
{
    for (var i = 1; i <= 8; i++)
    {
        if (b.ColumnCounts[i - 1] - b.CountColumn(i, CellState.Wall) == 0 && b.CountColumn(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[i, j] == CellState.Unknown)
                    b.State[i, j] = CellState.Empty;
            if (showOutput)
            {
                Console.WriteLine($"Column {i} has no more walls, marking as empty");
                Redraw(b);
            }
            return true;
        }

        if (b.ColumnCounts[i - 1] - b.CountColumn(i, CellState.Wall) == b.CountColumn(i, CellState.Unknown) && b.CountColumn(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[i, j] == CellState.Unknown)
                    b.State[i, j] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Column {i} has remaining unknowns equal to wall count, marking as walls");
                Redraw(b);
            }
            return true;
        }

        if (b.RowCounts[i - 1] - b.CountRow(i, CellState.Wall) == 0 && b.CountRow(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[j, i] == CellState.Unknown)
                    b.State[j, i] = CellState.Empty;
            if (showOutput)
            {
                Console.WriteLine($"Row {i} has no more walls, marking as empty");
                Redraw(b);
            }
            return true;
        }

        if (b.RowCounts[i - 1] - b.CountRow(i, CellState.Wall) == b.CountRow(i, CellState.Unknown) && b.CountRow(i, CellState.Unknown) > 0)
        {
            for (var j = 1; j <= 8; j++)
                if (b.State[j, i] == CellState.Unknown)
                    b.State[j, i] = CellState.Wall;
            if (showOutput)
            {
                Console.WriteLine($"Row {i} has remaining unknowns equal to wall count, marking as walls");
                Redraw(b);
            }
            return true;
        }
    }
    return false;
}

static bool CheckForPathOut(Board b, bool showOutput)
{
    for (var i = 1; i <= 8; i++)
        for (var j = 1; j <= 8; j++)
            switch (b.State[i, j])
            {
                case CellState.Monster:
                    var mn = b.GetNeighbors(i, j, CellState.Unknown);
                    if (mn.Count == 1 && b.CountNeighbors(i, j, CellState.Wall) == 3)
                    {
                        b.State[mn[0].X, mn[0].Y] = CellState.Empty;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) needs a way out, marking as empty");
                            Redraw(b);
                        }
                        return true;
                    }
                    else
                    {
                        mn = b.GetNeighbors(i, j, CellState.Empty);
                        if (mn.Count == 1)
                        {
                            mn = b.GetNeighbors(i, j, CellState.Unknown);

                            if (mn.Count > 0)
                            {
                                foreach (var m in mn)
                                    b.State[m.X, m.Y] = CellState.Wall;

                                if (showOutput)
                                {
                                    Console.WriteLine($"Cell ({i},{j}) can only have one way out, marking rest as walls");
                                    Redraw(b);
                                }
                                return true;
                            }
                        }
                    }

                    // add check for one left lines

                    break;
                case CellState.Empty:
                    var en = b.GetNeighbors(i, j, CellState.Unknown);
                    if (en.Count == 1 && b.CountNeighbors(i, j, CellState.Wall) == 2)
                    {
                        b.State[en[0].X, en[0].Y] = CellState.Empty;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) needs a way out, marking as empty");
                            Redraw(b);
                        }
                        return true;
                    }
                    break;
                case CellState.Unknown:
                    var un = b.GetNeighbors(i, j, CellState.Wall);
                    if (un.Count == 3)
                    {
                        b.State[i, j] = CellState.Wall;
                        if (showOutput)
                        {
                            Console.WriteLine($"Cell ({i},{j}) is a dead end, fill it in");
                            Redraw(b);
                        }
                        return true;
                    }
                    break;
            }
    return false;
}

