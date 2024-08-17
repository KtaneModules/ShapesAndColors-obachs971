using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PuzzleGenerator {
    private readonly int gridSize = 3;
    private readonly string letters = "RYB";
    private readonly string numbers = "CTD";
    private bool needUpdate = false;
    private string[][] TPsol;
    //private bool needLoop;
    // Returns an list of randomly generated clues
    public List<string[][]> getClues()
    {
        //looper:
        string[][] solution = getInitialSolution();
        TPsol = solution;

        List<int> positions = getShuffledPositions();
        List<List<List<string>>> possible = getInitialPossible();
        List<string[][]> clues = new List<string[][]>();
        foreach(int position in positions)
        {
            int row = position / gridSize, col = position % gridSize;
            if(possible[row][col].Count > 1)
            {
                string[][] clue = getBlankClue();
                clue[row][col] = getFirstClueElement(solution[row][col], possible[row][col]);
                blackOutSpaces(possible, clue, row, col);   // Gen 3
                clue = makeClueDistinct(possible, solution, clue);  // Gen 3.5
                removePossibleCombinations(possible, clue); //Gen 2
                removePossibleCombinations(possible);   // Gen 2
                clues.Add(clue);
            }
        }
        //needLoop = true;
        removeRedundantClues(clues);    // Gen 5
        removeRedundantClueElements(clues); // Gen 5
        removeRedundantClues(clues);    // Gen 5
        removeRedundantSpaces(clues);   // Gen 6
        removeRedundantClues(clues);    // Gen 5
        combineClues(clues, solution);  // Gen 4
        while (needUpdate)
        {
            //needLoop = true;
            needUpdate = false;
            removeRedundantClues(clues);    // Gen 5
            removeRedundantClueElements(clues); // Gen 5
            removeRedundantClues(clues);    // Gen 5
            removeRedundantSpaces(clues);   // Gen 6
            removeRedundantClues(clues);    // Gen 5
            combineClues(clues, solution);  // Gen 4
        }
        //needLoop = true;
        //canSolve(clues);
        //if (needLoop)
        //    goto looper;
        for (int i = 0; i < clues.Count; i++)
            clues[i] = shrinkClue(clues[i]);
        turnSpacesBlack(clues);         // Gen 7
        return clues;
    }
    public List<string[][]> debugClues(List<string[][]> clues, string[][] solution)
    {
        removeRedundantClues(clues);    // Gen 5
        removeRedundantClueElements(clues); // Gen 5
        removeRedundantClues(clues);    // Gen 5
        removeRedundantSpaces(clues);   // Gen 6
        removeRedundantClues(clues);    // Gen 5
        combineClues(clues, solution);  // Gen 4
        while (needUpdate)
        {
            needUpdate = false;
            removeRedundantClues(clues);    // Gen 5
            removeRedundantClueElements(clues); // Gen 5
            removeRedundantClues(clues);    // Gen 5
            removeRedundantSpaces(clues);   // Gen 6
            removeRedundantClues(clues);    // Gen 5
            combineClues(clues, solution);  // Gen 4
        }
        //if (needLoop)
        //    goto looper;
        for (int i = 0; i < clues.Count; i++)
            clues[i] = shrinkClue(clues[i]);
        turnSpacesBlack(clues);         // Gen 7
        return clues;
    }
    //This is for TP autosolve
    public string[][] getSolution()
    {
        return TPsol;
    }
    // Returns a randomly generated solution
    private string[][] getInitialSolution()
    {
        List<string> choices = new List<string>();
        foreach (char let in letters)
        {
            foreach (char num in numbers)
                choices.Add(let + "" + num);
        }
        choices.Shuffle();
        string[][] solution = new string[gridSize][];
        for (int i = 0; i < gridSize; i++)
            solution[i] = new string[gridSize];
        for (int i = 0; i < choices.Count; i++)
            solution[i / gridSize][i % gridSize] = choices[i];
        return solution;
    }
    // Returns a list of shuffled positions
    private List<int> getShuffledPositions()
    {
        List<int> positions = new List<int>();
        for (int i = 0; i < (gridSize * gridSize); i++)
            positions.Add(i);
        positions.Shuffle();
        return positions;
    }

    // Returns all possible combinations for each space in the grid
    private List<List<List<string>>> getInitialPossible()
    {
        List<List<List<string>>> possible = new List<List<List<string>>>();
        for(int i = 0; i < gridSize; i++)
        {
            possible.Add(new List<List<string>>());
            for(int j = 0; j < gridSize; j++)
            {
                possible[i].Add(new List<string>());
                foreach(char let in letters)
                {
                    foreach (char num in numbers)
                        possible[i][j].Add(let + "" + num);
                }
            }
        }
        return possible;
    }

    // Returns a blank clue grid
    private string[][] getBlankClue()
    {
        string[][] clue = new string[gridSize][];
        for(int i = 0; i < gridSize; i++)
        {
            clue[i] = new string[gridSize];
            for (int j = 0; j < gridSize; j++)
                clue[i][j] = "WW";
        }
        return clue;
    }
    // Returns a clue element that reduces the number of possible combinations down to 1
    private string getFirstClueElement(string solution, List<string> possible)
    {
        List<string> choices = new List<string>();
        choices.Add(solution[0] + "");
        choices.Add(solution[1] + "");
        foreach (char c in letters.Replace(solution[0] + "", ""))
            choices.Add("-" + c);
        foreach (char c in numbers.Replace(solution[1] + "", ""))
            choices.Add("-" + c);
        choices.Shuffle();
        foreach(string choice in choices)
        {
            if (canUseClueElement(possible, choice))
                return choice;
        }
        return solution.ToUpperInvariant();
    }
    // Checks if the clue element reduces the number of possible combinations down to 1
    private bool canUseClueElement(List<string> possible, string element)
    {
        int sum = 0;
        if(element[0] == '-')
        {
            foreach(string str in possible)
            {
                if (!(str.Contains(element.Substring(1))))
                    sum++;
            }
        }
        else
        {
            foreach (string str in possible)
            {
                if (str.Contains(element))
                    sum++;
            }
        }
        return sum == 1;
    }
    // Removes possible combinations using the given clue
    private void removePossibleCombinations(List<List<List<string>>> possible, string[][] clue)
    {
        for(int i = 0; i < gridSize; i++)
        {
            for(int j = 0; j < gridSize; j++)
            {
                if(!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    if(clue[i][j][0] == '-')
                    {
                        for(int k = 0; k < possible[i][j].Count; k++)
                        {
                            if (possible[i][j][k].Contains(clue[i][j].Substring(1)))
                                possible[i][j].RemoveAt(k--);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < possible[i][j].Count; k++)
                        {
                            if (!(possible[i][j][k].Contains(clue[i][j])))
                                possible[i][j].RemoveAt(k--);
                        }
                    }
                }
            }
        }
    }
    // Removes possible combinations that have been reduced down to 1 spot on the grid
    private void removePossibleCombinations(List<List<List<string>>> possible)
    {
        bool flag = true;
        while (flag)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            for (int row = 0; row < possible.Count; row++)
            {
                for (int col = 0; col < possible[row].Count; col++)
                {
                    string temp = string.Join(" ", possible[row][col].ToArray());
                    if (dict.ContainsKey(temp))
                        dict[temp]++;
                    else
                        dict.Add(temp, 1);   
                }
            }
            flag = false;
            foreach (string str in dict.Keys)
            {
                string[] list = str.Split(' ');
                if (list.Length == dict[str])
                {
                    for (int i = 0; i < gridSize; i++)
                    {
                        for (int j = 0; j < gridSize; j++)
                        {
                            if (!(list.SequenceEqual(possible[i][j].ToArray())))
                            {
                                for (int k = 0; k < list.Length; k++)
                                {
                                    if (possible[i][j].Remove(list[k]))
                                        flag = true;
                                }
                            }

                        }
                    }
                }
            }
        }
    }
    // Blacks out spaces on the clue based on what is filled around the clue
    private void blackOutSpaces(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        int cur = gridSize - 1;
        for(int i = (row + 1); i < gridSize; i++)
        {
            if (possible[i][col].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[cur][j] = "KK";
                cur--;
            }
            else
                break;
        }
        cur = gridSize - 1;
        for(int i = (col + 1); i < gridSize; i++)
        {
            if (possible[row][i].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[j][cur] = "KK";
                cur--;
            }
            else
                break;
        }
        cur = 0;
        for (int i = (row - 1); i >= 0; i--)
        {
            if (possible[i][col].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[cur][j] = "KK";
                cur++;
            }
            else
                break;
        }
        cur = 0;
        for (int i = (col - 1); i >= 0; i--)
        {
            if (possible[row][i].Count == 1)
            {
                for (int j = 0; j < gridSize; j++)
                    clue[j][cur] = "KK";
                cur++;
            }
            else
                break;
        }
    }

    // Makes the clue distinct, AKA ensures that the clue can only be placed in exactly 1 spot onto the grid
    private string[][] makeClueDistinct(List<List<List<string>>> possible, string[][] solution, string[][] clue)
    {
        bool flag = isDistinct(possible, clue);
        while(!(flag))
        {
            List<string[][]> choices = new List<string[][]>();
            for(int i = 0; i < gridSize; i++)
            {
                for(int j = 0; j < gridSize; j++)
                {
                    if(clue[i][j].Equals("WW"))
                    {
                        List<string> elements = new List<string>();
                        elements.Add(solution[i][j][0] + "");
                        elements.Add(solution[i][j][1] + "");
                        foreach (char c in letters.Replace(solution[i][j][0] + "", ""))
                            elements.Add("-" + c);
                        foreach (char c in numbers.Replace(solution[i][j][1] + "", ""))
                            elements.Add("-" + c);
                        foreach(string element in elements)
                        {
                            string[][] temp = copyArray(clue);
                            temp[i][j] = element.ToUpperInvariant();
                            choices.Add(temp);
                        }
                    }
                }
            }
            choices.Shuffle();
            foreach(string[][] choice in choices)
            {
                if (isDistinct(possible, choice))
                    return choice;
            }
            List<int[]> pos = new List<int[]>();
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (clue[i][j].Equals("KK"))
                    {
                        if (i > 0 && !(clue[i - 1][j].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (j > 0 && !(clue[i][j - 1].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (i < (gridSize - 1) && !(clue[i + 1][j].Equals("KK")))
                            pos.Add(new int[] { i, j });
                        else if (j < (gridSize - 1) && !(clue[i][j + 1].Equals("KK")))
                            pos.Add(new int[] { i, j });
                    }
                }
            }
            pos.Shuffle();
            clue[pos[0][0]][pos[0][1]] = "WW";
            int[] min = getMinArea(clue);
            for (int i = min[0]; i <= min[2]; i++)
            {
                for (int j = min[3]; j <= min[1]; j++)
                {
                    if (clue[i][j].Equals("KK"))
                        clue[i][j] = "WW";
                }
            }
            flag = isDistinct(possible, clue);
        }
        return clue;
    }

    // Checks if the clue can be placed exactly once
    private bool isDistinct(List<List<List<string>>> possible, string[][] clue)
    {
        string[][] temp = shrinkClue(clue);
        int sum = 0;
        for (int i = 0; i < (gridSize - temp.Length + 1); i++)
        {
            for (int j = 0; j < (gridSize - temp[0].Length + 1); j++)
            {
                if (canBePlaced(possible, temp, i, j))
                    sum++;
            }
        }
        return (sum == 1);
    }
    // Checks if the clue can be placed onto the grid at that spot
    private bool canBePlaced(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        for (int i = 0; i < clue.Length; i++)
        {
            for (int j = 0; j < clue[i].Length; j++)
            {
                if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    bool flag = true;
                    if (clue[i][j][0] == '-')
                    {
                        foreach(string str in possible[i + row][j + col])
                        {
                            if (!(str.Contains(clue[i][j].Substring(1))))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach(string str in possible[i + row][j + col])
                        {
                            if (str.Contains(clue[i][j]))
                            {
                                flag = false;
                                break;
                            }
                        }
                    }
                    if (flag)
                        return false;
                }
            }
        }
        return !(isContradicting(possible, clue, row, col));
    }
    // Checks to see if placing the clue at the specific spot would cause a contradiction
    private bool isContradicting(List<List<List<string>>> possible, string[][] clue, int row, int col)
    {
        List<List<List<string>>> copy = copyList(possible);
        for (int i = 0; i < clue.Length; i++)
        {
            for (int j = 0; j < clue[i].Length; j++)
            {
                if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
                {
                    if (clue[i][j][0] == '-')
                    {
                        for (int k = 0; k < copy[i + row][j + col].Count; k++)
                        {
                            if (copy[i + row][j + col][k].Contains(clue[i][j].Substring(1)))
                                copy[i + row][j + col].RemoveAt(k--);
                        }
                    }
                    else
                    {
                        for (int k = 0; k < copy[i + row][j + col].Count; k++)
                        {
                            if (!(copy[i + row][j + col][k].Contains(clue[i][j])))
                                copy[i + row][j + col].RemoveAt(k--);
                        }
                    }
                }
            }
        }
        Dictionary<string, int> dict = new Dictionary<string, int>();
        for (int r = 0; r < copy.Count; r++)
        {
            for (int c = 0; c < copy[r].Count; c++)
            {
                string temp = string.Join("", copy[r][c].ToArray());
                if (dict.ContainsKey(temp))
                    dict[temp]++;
                else
                    dict.Add(temp, 1);
            }
        }
        foreach (string str in dict.Keys)
        {
            if ((str.Length / 2) < dict[str])
                return true;
        }
        return false;
    }
    // Shrinks the clue, removing any KK spaces around it
    private string[][] shrinkClue(string[][] clue)
    {
        int[] min = getMinArea(clue);
        string[][] shrunk = new string[min[2] - min[0] + 1][];
        for (int i = 0; i < shrunk.Length; i++)
        {
            shrunk[i] = new string[min[1] - min[3] + 1];
            for (int j = 0; j < shrunk[i].Length; j++)
                shrunk[i][j] = clue[min[0] + i][min[3] + j].ToUpperInvariant();
        }
        return shrunk;
    }

    // Returns the minimal area required for the clue
    private int[] getMinArea(string[][] clue)
    {
        int top = -1, right = -1, bottom = -1, left = -1;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!(clue[i][j].Equals("KK")))
                {
                    top = i;
                    break;
                }
            }
            if (top > -1)
                break;
        }
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (!(clue[j][i].Equals("KK")))
                {
                    left = i;
                    break;
                }
            }
            if (left > -1)
                break;
        }
        for (int i = gridSize - 1; i >= 0; i--)
        {
            for (int j = gridSize - 1; j >= 0; j--)
            {
                if (!(clue[i][j].Equals("KK")))
                {
                    bottom = i;
                    break;
                }
            }
            if (bottom > -1)
                break;
        }
        for (int i = gridSize - 1; i >= 0; i--)
        {
            for (int j = gridSize - 1; j >= 0; j--)
            {
                if (!(clue[j][i].Equals("KK")))
                {
                    right = i;
                    break;
                }
            }
            if (right > -1)
                break;
        }
        return new int[] { top, right, bottom, left };
    }

    // Combines all the clues in the list if it can
    private void combineClues(List<string[][]> clues, string[][] solution)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            for (int j = i + 1; j < clues.Count; j++)
            {
                if (canCombine(clues[i], clues[j]))
                {
                    for (int row = 0; row < gridSize; row++)
                    {
                        for (int col = 0; col < gridSize; col++)
                        {
                            if (clues[i][row][col].Equals("WW"))
                                clues[i][row][col] = clues[j][row][col];
                            else if (!(clues[j][row][col].Equals("WW") || clues[j][row][col].Equals("KK")))
                            {
                                if (clues[i][row][col][0] == '-')
                                {
                                    if (!(clues[i][row][col].Substring(1).Equals(clues[j][row][col].Substring(1))))
                                        clues[i][row][col] = solution[row][col][getType(clues[i][row][col].Substring(1))] + "";
                                }
                                else if (clues[j][row][col].Length > clues[i][row][col].Length)
                                    clues[i][row][col] = clues[j][row][col];
                                else if (clues[i][row][col].Length == 1 && clues[j][row][col].Length == 1)
                                {
                                    if (getType(clues[i][row][col]) != getType(clues[j][row][col]))
                                        clues[i][row][col] = solution[row][col];
                                }
                            }
                        }
                    }
                    clues.RemoveAt(j--);
                    needUpdate = true;
                }
            }
        }
    }

    // Checks if the 2 clues can combine into 1 clue
    private bool canCombine(string[][] c1, string[][] c2)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (c1[i][j].Equals("KK") && !(c2[i][j].Equals("KK")))
                    return false;
                else if (c2[i][j].Equals("KK") && !(c1[i][j].Equals("KK")))
                    return false;
                else if (!(c1[i][j].Equals("KK")) && !(c1[i][j].Equals("WW")) && !(c2[i][j].Equals("KK")) && !(c2[i][j].Equals("WW")))
                {
                    if (c1[i][j][0] == '-' && c2[i][j][0] != '-')
                        return false;
                    else if (c1[i][j][0] != '-' && c2[i][j][0] == '-')
                        return false;
                    else if (c1[i][j][0] == '-' && c2[i][j][0] == '-')
                    {
                        if (getType(c1[i][j].Substring(1)) != getType(c2[i][j].Substring(1)))
                            return false;
                    }
                }
            }
        }
        return true;
    }
    // Removes any clues that are not needed for the Puzzle
    private void removeRedundantClues(List<string[][]> clues)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            string[][] removed = copyArray(clues[i]);
            clues.RemoveAt(i);
            if (!(canSolve(clues)))
                clues.Insert(i, removed);
            else
            {
                i--;
                needUpdate = true;
            }
        }
    }
    // Checks if the puzzle can be solved using the list of clues it is provided
    private bool canSolve(List<string[][]> clues)
    {
        List<List<List<string>>> possible = getInitialPossible();
        List<string[][]> used = new List<string[][]>();
        bool flag = true;
        while (flag)
        {
            flag = false;
            foreach(string[][] clue in clues)
            {
                if (!(used.Contains(clue)) && isDistinct(possible, clue))
                {
                    used.Add(clue);
                    removePossibleCombinations(possible, clue);
                    removePossibleCombinations(possible);
                    flag = true;
                    break;
                }
            }
        }
        foreach(List<List<string>> row in possible)
        {
            foreach(List<string> col in row)
            {
                if (col.Count > 1)
                    return false;
            }
        }
        return true;
    }
    // Removes redundant Clue Elements from each clue
    private void removeRedundantClueElements(List<string[][]> clues)
    {
        clues.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            List<int[]> posList = new List<int[]>();
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (!(clues[i][row][col].Equals("KK") || clues[i][row][col].Equals("WW")))
                        posList.Add(new int[] { row, col });
                }
            }
            posList.Shuffle();
            foreach(int[] pos in posList)
            {
                string[][] temp = copyArray(clues[i]);
                clues[i][pos[0]][pos[1]] = "WW";
                if (!(canSolve(clues)))
                {
                    if (temp[pos[0]][pos[1]][0] != '-' && temp[pos[0]][pos[1]].Length == 2)
                    {
                        List<char> choices = new List<char>();
                        choices.Add(temp[pos[0]][pos[1]][0]);
                        choices.Add(temp[pos[0]][pos[1]][1]);
                        choices.Shuffle();
                        bool flag = true;
                        foreach (char c in choices)
                        {
                            clues[i][pos[0]][pos[1]] = c + "";
                            if (canSolve(clues))
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag)
                            clues[i] = temp;
                        else
                            needUpdate = true;
                    }
                    else
                        clues[i] = temp;
                }
                else
                    needUpdate = true;
            }
        }
    }
    // Removes redundant spaces from each clue
    private void removeRedundantSpaces(List<string[][]> clues)
    {
        clues.Shuffle();
        List<int> types = new List<int>();
        for (int i = 0; i < 4; i++)
            types.Add(i);
        types.Shuffle();
        for (int i = 0; i < clues.Count; i++)
        {
            for(int j = 0; j < types.Count; j++)
            {
                bool flag = true;
                int[] minArea = getMinArea(clues[i]);  // Top, Right, Bottom, Left
                if (types[j] % 2 == 0)
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        if (!(clues[i][minArea[types[j]]][k].Equals("WW") || clues[i][minArea[types[j]]][k].Equals("KK")))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                else
                {
                    for (int k = 0; k < gridSize; k++)
                    {
                        if (!(clues[i][k][minArea[types[j]]].Equals("WW") || clues[i][k][minArea[types[j]]].Equals("KK")))
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if (flag)
                {
                    string[][] temp = copyArray(clues[i]);
                    if (types[j] % 2 == 0)
                    {
                        for (int k = 0; k < gridSize; k++)
                            clues[i][minArea[types[j]]][k] = "KK";
                    }
                    else
                    {
                        for (int k = 0; k < gridSize; k++)
                            clues[i][k][minArea[types[j]]] = "KK";
                    }
                    if (!(canSolve(clues)))
                        clues[i] = temp;
                    else
                    {
                        needUpdate = true;
                        j--;
                    }
                        
                }
            }
        }
    }

    // Randomly turns white spaces into black spaces
    private void turnSpacesBlack(List<string[][]> clues)
    {
        for(int i = 0; i < clues.Count; i++)
        {
            List<int[]> positions = new List<int[]>();
            for(int row = 0; row < clues[i].Length; row++)
            {
                for(int col = 0; col < clues[i][row].Length; col++)
                {
                    if (clues[i][row][col].Equals("WW") && canTurnBlack(clues[i], row, col))
                        positions.Add(new int[] { row, col });
                }
            }
            positions.Shuffle();
            foreach(int[] position in positions)
            {
                if (canTurnBlack(clues[i], position[0], position[1]) && Random.Range(0, 2) == 0)
                    clues[i][position[0]][position[1]] = "KK";
            }
        }
    }
    //This checks if the space can be turned black.
    private bool canTurnBlack(string[][] clue, int row, int col)
    {
        string[][] temp = new string[gridSize][];
        for (int i = 0; i < gridSize; i++)
        {
            temp[i] = new string[gridSize];
            for (int j = 0; j < gridSize; j++)
            {
                if (i < clue.Length && j < clue[i].Length)
                    temp[i][j] = clue[i][j] + "";
                else
                    temp[i][j] = "KK";
            }
        }
        //Checks if they are the same size when turning that space black
        temp[row][col] = "KK";
        temp = shrinkClue(temp);
        if (temp.Length == clue.Length && temp[0].Length == clue[0].Length)
        {
            //Checking if the spaces are connected
            row = -1; col = -1;
            for (int i = 0; i < temp.Length; i++)
            {
                for (int j = 0; j < temp[i].Length; j++)
                {
                    if (!(temp[i][j].Equals("KK")))
                    {
                        row = i;
                        col = j;
                        temp[i][j] = "KK";
                        break;
                    }
                }
                if (row >= 0)
                    break;
            }
            string dir = "";
            while (true)
            {
                if (row > 0 && !(temp[row - 1][col].Equals("KK")))
                {
                    temp[--row][col] = "KK";
                    dir += "U";
                }
                else if (row < (temp.Length - 1) && !(temp[row + 1][col].Equals("KK")))
                {
                    temp[++row][col] = "KK";
                    dir += "D";
                }
                else if (col > 0 && !(temp[row][col - 1].Equals("KK")))
                {
                    temp[row][--col] = "KK";
                    dir += "L";
                }
                else if (col < (temp[row].Length - 1) && !(temp[row][col + 1].Equals("KK")))
                {
                    temp[row][++col] = "KK";
                    dir += "R";
                }
                else
                {
                    if (dir.Length == 0)
                        break;
                    switch (dir[dir.Length - 1])
                    {
                        case 'U':
                            row++;
                            break;
                        case 'D':
                            row--;
                            break;
                        case 'L':
                            col++;
                            break;
                        case 'R':
                            col--;
                            break;
                    }
                    dir = dir.Substring(0, dir.Length - 1);
                }
            }
            for (int i = 0; i < temp.Length; i++)
            {
                for (int j = 0; j < temp[i].Length; j++)
                {
                    if (!(temp[i][j].Equals("KK")))
                        return false;
                }
            }
            return true;
        }
        return false;
    }
    // Copies the array
    private string[][] copyArray(string[][] arr)
    {
        string[][] copy = new string[arr.Length][];
        for (int i = 0; i < arr.Length; i++)
        {
            copy[i] = new string[arr[i].Length];
            for (int j = 0; j < arr[i].Length; j++)
                copy[i][j] = arr[i][j].ToUpperInvariant();
        }
        return copy;
    }
    // Copies the array
    private List<List<List<string>>> copyList(List<List<List<string>>> list)
    {
        List<List<List<string>>> copy = new List<List<List<string>>>();
        foreach(List<List<string>> row in list)
        {
            copy.Add(new List<List<string>>());
            foreach(List<string> col in row)
            {
                copy[copy.Count - 1].Add(new List<string>());
                foreach (string str in col)
                    copy[copy.Count - 1][copy[copy.Count - 1].Count - 1].Add(str.ToUpperInvariant());
            }
        }
        return copy;
    }
    // Returns the clue type
    private int getType(string c)
    {
        if (letters.Contains(c))
            return 0;
        else if (numbers.Contains(c))
            return 1;
        return 2;
    }
}
