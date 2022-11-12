using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class shapesAndColors : MonoBehaviour {
	private static readonly int NUM_ROW = 3;
	private static readonly int NUM_COL = 3;
	public KMBombModule module;
	public new KMAudio audio;
	public KMSelectable ClueUp;
	public KMSelectable ClueDown;
	public KMSelectable[] colorInput;
	public KMSelectable[] shapeInput;
	public KMSelectable[] grid;
	public KMSelectable submit;
	public MeshRenderer[] gridMeshRender;
	public MeshRenderer colorSelector;
	public MeshRenderer shapeSelector;
	public MeshRenderer[] clueMeshRender;
	public MeshRenderer[] backSpaces;
	public Material[] images;
	public AudioClip LoadingSFX;
	public AudioClip BlinkSFX;
	public AudioClip StampSFX;
	public AudioClip PeelSFX;
	public AudioClip SelectSFX;
	public AudioClip[] Arrows;
	private List<Material[]> clues;
	private List<string[][]> textClues;
	private int clueCursor;
	private float[] selectPositions = { 0.025f, -0.005f, -0.035f };
	private int moduleId;
	private static int moduleIdCounter = 1;
	private string[][] solution;
	private string[][] submission;
	private int colorCursor = -1;
	private int shapeCursor = -1;
	private bool notStart = false;

	//private bool doLoop = true;

	void Awake()
	{
		moduleId = moduleIdCounter++;
	}
	void Start()
	{
		StartCoroutine(generatePuzzle());
	}
	private IEnumerator generatePuzzle()
	{
		yield return new WaitForSeconds(0f);
		submission = new string[][] { new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" } };
		string[] shuffler = { "RC", "RT", "RD", "YC", "YT", "YD", "BC", "BT", "BD" };
		shuffler.Shuffle();
		solution = new string[3][] { new string[3], new string[3], new string[3] };
		for (int i = 0; i < shuffler.Length; i++)
			solution[i / 3][i % 3] = shuffler[i].ToUpperInvariant();
		//Debug.LogFormat("[Shapes and Colors #{0}] Solution:", moduleId);
		//foreach (string[] arr in solution)
			//Debug.LogFormat("[Shapes and Colors #{0}] {1} {2} {3}", moduleId, arr[0], arr[1], arr[2]);
		
		//loop:
		textClues = generateClues().Shuffle();
		clueCursor = 0;
		clues = new List<Material[]>();
		foreach (string[][] clue in textClues)
		{
			clues.Add(new Material[9]);
			for (int i = 0; i < clues[clues.Count - 1].Length; i++)
			{
				if ((i / 3) >= clue.Length || (i % 3) >= clue[i / 3].Length)
					clues[clues.Count - 1][i] = images[1];
				else
                {
					//Debug.LogFormat("[Shapes and Colors #{0}] Space Check: {1}", moduleId, clue[i / 3][i % 3]);
					clues[clues.Count - 1][i] = images[getMat(clue[i / 3][i % 3])];
				}
					
			}
		}
		/*
		if(doLoop)
			goto loop;
		*/
		if (notStart)
		{
			foreach (MeshRenderer space in gridMeshRender)
			{
				space.material = images[0];
				space.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
			}
			audio.PlaySoundAtTransform(LoadingSFX.name, transform);
			for (int i = 0; i < 17; i++)
			{
				foreach (MeshRenderer mesh in clueMeshRender)
					mesh.material = images[UnityEngine.Random.Range(0, images.Length)];
				yield return new WaitForSeconds(0.1f);
			}
			for (int i = 0; i < 3; i++)
			{
				audio.PlaySoundAtTransform(BlinkSFX.name, transform);
				displayClue();
				yield return new WaitForSeconds(0.25f);
				foreach (MeshRenderer mesh in clueMeshRender)
					mesh.material = images[1];
				yield return new WaitForSeconds(0.25f);
			}
			audio.PlaySoundAtTransform(BlinkSFX.name, transform);
		}
		displayClue();
		int[] indexes = { 0, 1, 2 };
		ClueUp.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[0].name, transform); clueCursor = mod(clueCursor - 1, clues.Count); displayClue(); return false; };
		ClueDown.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[1].name, transform); clueCursor = mod(clueCursor + 1, clues.Count); displayClue(); return false; };
		submit.OnInteract = delegate { StartCoroutine(pressedSubmit()); return false; };
		foreach (int index in indexes)
		{
			colorInput[index].OnInteract = delegate { selectColor(index); return false; };
			shapeInput[index].OnInteract = delegate { selectShape(index); return false; };
		}
		indexes = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
		foreach (int index in indexes)
			grid[index].OnInteract = delegate { pressedGrid(index); return false; };
		notStart = true;
	}
	private void selectColor(int cursor)
	{
		audio.PlaySoundAtTransform(SelectSFX.name, transform);
		if (cursor == colorCursor)
		{
			colorCursor = -1;
			colorSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		}
		else
		{
			colorCursor = cursor;
			colorSelector.transform.localPosition = new Vector3(0.035f, 0.0154f, selectPositions[colorCursor]);
		}
	}
	private void selectShape(int cursor)
	{
		audio.PlaySoundAtTransform(SelectSFX.name, transform);
		if (cursor == shapeCursor)
		{
			shapeCursor = -1;
			shapeSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		}
		else
		{
			shapeCursor = cursor;
			shapeSelector.transform.localPosition = new Vector3(0.065f, 0.0154f, selectPositions[shapeCursor]);
		}
	}
	private void pressedGrid(int i)
	{
		string combo = "WW";
		if (colorCursor >= 0)
			combo = "RYB"[colorCursor] + "" + combo[1];
		if(shapeCursor >= 0)
			combo = combo[0] + "" + "CTD"[shapeCursor];
		string place;
		if (submission[i / 3][i % 3].Equals(combo) || combo.Equals("WW"))
			place = "WW";
		else if (combo[0] == 'W')
		{
			if (combo[1] == submission[i / 3][i % 3][1])
				place = submission[i / 3][i % 3][0] + "W";
			else
				place = submission[i / 3][i % 3][0] + "" + combo[1];
		}
		else if (combo[1] == 'W')
		{
			if (combo[0] == submission[i / 3][i % 3][0])
				place = "W" + submission[i / 3][i % 3][1];
			else
				place = combo[0] + "" + submission[i / 3][i % 3][1];
		}
		else
			place = combo.ToUpperInvariant();
		gridMeshRender[i].material = images[getMat(place)];
		if ((place[0] == 'W' && submission[i / 3][i % 3][0] != 'W') || (place[1] == 'W' && submission[i / 3][i % 3][1] != 'W'))
			audio.PlaySoundAtTransform(PeelSFX.name, transform);
		else if(!(place.Equals("WW")))
		{
			grid[i].AddInteractionPunch();
			audio.PlaySoundAtTransform(StampSFX.name, transform);
		}
			
		submission[i / 3][i % 3] = place.ToUpperInvariant();
	}
	private IEnumerator pressedSubmit()
	{
		ClueUp.OnInteract = null;
		ClueDown.OnInteract = null;
		submit.OnInteract = null;
		foreach (KMSelectable input in colorInput)
			input.OnInteract = null;
		foreach (KMSelectable input in shapeInput)
			input.OnInteract = null;
		foreach (KMSelectable input in grid)
			input.OnInteract = null;
		colorSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		shapeSelector.transform.localPosition = new Vector3(0f, 0f, 0f);
		colorCursor = -1;
		shapeCursor = -1;
		List<int> notFilled = new List<int>();
		//Debug.LogFormat("[Shapes and Colors #{0}] User Submission:", moduleId);
		for(int i = 0; i < submission.Length; i++)
		{
			string submitLog = "";
			for (int j = 0; j < submission[i].Length; j++)
			{
				if (submission[i][j].Contains("W"))
					notFilled.Add(i * 3 + j);
				submitLog = submitLog + "" + submission[i][j] + " ";
			}
			//Debug.LogFormat("[Shapes and Colors #{0}] {1}", moduleId, submitLog);
		}
		if(notFilled.Count == 0)
		{
			//Next, check if all the clues can fit on the grid at least once.
			bool flag = true;
			clueCursor = -1;
			foreach(string[][] clue in textClues)
			{
				clueCursor++;
				displayClue();
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				yield return new WaitForSeconds(0.5f);
				for (int i = 0; i <= (submission.Length - clue.Length); i++)
				{
					for (int j = 0; j <= (submission[i].Length - clue[i % clue.Length].Length); j++)
					{
						flag = true;
						List<int> spacesToLight = new List<int>();
						for (int a = 0; a < clue.Length; a++)
						{
							for (int b = 0; b < clue[a].Length; b++)
							{
								if (!(clue[a][b].Equals("KK")) && !(clue[a][b].Equals("WW")) && !(doesFit(submission[i + a][j + b], clue[a][b])))
								{
									flag = false;
									goto skip1;
								}
								if(!(clue[a][b].Equals("KK")))
									spacesToLight.Add(((i + a) * 3) + (j + b));
							}
						}
						skip1:
						if(flag)
						{
							audio.PlaySoundAtTransform(ClueUp.name, transform);
							foreach (int space in spacesToLight)
								backSpaces[space].material = images[23];
							yield return new WaitForSeconds(0.5f);
							foreach (MeshRenderer backSpace in backSpaces)
								backSpace.material = images[1];
							goto skip2;
						}
					}
				}
				skip2:
				if(!(flag))
				{
					//Debug.LogFormat("[Shapes and Colors #{0}] Strike! One of the clues could not fit onto the grid! Generating a new puzzle.", moduleId);
					module.HandleStrike();
					foreach(MeshRenderer backSpace in backSpaces)
						backSpace.material = images[getMat("R")];
					yield return new WaitForSeconds(5.0f);
					foreach (MeshRenderer backSpace in backSpaces)
						backSpace.material = images[1];
					StartCoroutine(generatePuzzle());
					goto end;
				}
			}
			//Finally, check if the grid has one of each color/shape combo
			foreach (MeshRenderer clueSpace in clueMeshRender)
				clueSpace.material = images[1];
			audio.PlaySoundAtTransform(ClueDown.name, transform);
			yield return new WaitForSeconds(0.5f);
			string[] comboList = { "RC", "RT", "RD", "YC", "YT", "YD", "BC", "BT", "BD"};
			
			List<int> missed = new List<int>();
			for(int i = 0; i < comboList.Length; i++)
			{
				int cur = -1;
				for(int j = 0; j < submission.Length; j++)
				{
					for(int k = 0; k < submission[j].Length; k++)
					{
						if(submission[j][k].Equals(comboList[i]))
						{
							cur = (j * 3) + k;
							goto skip3;
						}
					}
				}
				skip3:
				if (cur >= 0)
				{
					audio.PlaySoundAtTransform(StampSFX.name, transform);
					gridMeshRender[cur].transform.localScale = new Vector3(0f, 0f, 0f);
					clueMeshRender[i].material = images[getMat(comboList[i])];
					submission[cur / 3][cur % 3] = "WW";
				}
				else
					missed.Add(i);
				yield return new WaitForSeconds(0.5f);
			}
			if(missed.Count == 0)
			{
				foreach (MeshRenderer space in gridMeshRender)
					space.transform.localScale = new Vector3(0f, 0f, 0f);
				module.HandlePass();
				audio.PlaySoundAtTransform(ClueUp.name, transform);
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				yield return new WaitForSeconds(0.25f);
				audio.PlaySoundAtTransform(ClueUp.name, transform);
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				yield return new WaitForSeconds(0.125f);
				audio.PlaySoundAtTransform(ClueUp.name, transform);
				audio.PlaySoundAtTransform(ClueDown.name, transform);
				yield return new WaitForSeconds(0.125f);
				audio.PlaySoundAtTransform(ClueUp.name, transform);
				audio.PlaySoundAtTransform(ClueDown.name, transform);
			}
			else
			{
				//Debug.LogFormat("[Shapes and Colors #{0}] Strike! User tried to submit a grid with duplicates! Generating a new puzzle.", moduleId);
				module.HandleStrike();
				foreach (int blank in missed)
					clueMeshRender[blank].material = images[getMat("R")];
				for(int i = 0; i < submission.Length; i++)
				{
					for(int j = 0; j < submission[i].Length; j++)
					{
						if (!(submission[i][j].Equals("WW")))
							backSpaces[i * 3 + j].material = images[getMat("R")];
					}
				}
				yield return new WaitForSeconds(3.0f);
				foreach (MeshRenderer backSpace in backSpaces)
					backSpace.material = images[1];
				StartCoroutine(generatePuzzle());
			}
		}
		else
		{
			//Debug.LogFormat("[Shapes and Colors #{0}] Strike! User tried to submit an incomplete grid! No new puzzle needed.", moduleId);
			module.HandleStrike();
			foreach (int space in notFilled)
				backSpaces[space].material = images[getMat("R")];
			yield return new WaitForSeconds(1.0f);
			foreach (int space in notFilled)
				backSpaces[space].material = images[1];
			ClueUp.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[0].name, transform); clueCursor = mod(clueCursor - 1, clues.Count); displayClue(); return false; };
			ClueDown.OnInteract = delegate { audio.PlaySoundAtTransform(Arrows[1].name, transform); clueCursor = mod(clueCursor + 1, clues.Count); displayClue(); return false; };
			submit.OnInteract = delegate { StartCoroutine(pressedSubmit()); return false; };
			int[] indexes = { 0, 1, 2 };
			foreach (int index in indexes)
			{
				colorInput[index].OnInteract = delegate { selectColor(index); return false; };
				shapeInput[index].OnInteract = delegate { selectShape(index); return false; };
			}
			indexes = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
			foreach (int index in indexes)
				grid[index].OnInteract = delegate { pressedGrid(index); return false; };
		}
		end:
		yield return new WaitForSeconds(0f);
	}
	//This generates the clues.
	//1st: Randomize the order of the spaces to give clues for.
	//2nd: For each space in that order that has more than 1 possibility for a shape/color combo
	//create a clue for that space.
	//3rd: If there are 2 other shapes/colors already placed on the grid, place the same shape/color
	//without the other info. If they are tied, pick one of them randomly. Otherwise, use the 
	//shape/color combo for that clue.
	//4th: Remove the spaces around it if there are other shape/color combos filled in around it
	//5th: Finalize the clue by making sure the amount of places you can fit that clue on the grid is 1
	//6th: Add the clue and solve the grid accordingly
	//7th: After all the clues are generated, some of them are combined into a single clue.
	//8th: Shrink each clue to remove any unneeded black space
	//9th: Finally, randomly remove white spaces to create interesting shapes
	private List<string[][]> generateClues()
	{
		List<List<List<string>>> poss = new List<List<List<string>>>();
		string[] list = { "RC", "RT", "RD", "YC", "YT", "YD", "BC", "BT", "BD" };
		for (int i = 0; i < 3; i++)
		{
			poss.Add(new List<List<string>>());
			for(int j = 0; j < 3; j++)
			{
				poss[i].Add(new List<string>());
				foreach (string combo in list)
					poss[i][j].Add(combo.ToUpperInvariant());
			}
		}
		int[] clueOrder = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
		clueOrder.Shuffle();
		List<string[][]> clues = new List<string[][]>();
		foreach(int cluePos in clueOrder)
		{
			int row = cluePos / 3, col = cluePos % 3;
			if(poss[row][col].Count > 1)
			{
				string[][] clue = { new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" }, new string[] { "WW", "WW", "WW" } };
				List<string> choice = new List<string>();
				if (getSum(poss[row][col], solution[row][col][0] + "") == 1)
					choice.Add("" + solution[row][col][0]);
				if (getSum(poss[row][col], solution[row][col][1] + "") == 1)
					choice.Add("" + solution[row][col][1]);
				if (choice.Count() == 0)
					choice.Add(solution[row][col].ToUpperInvariant());
				string negatives = "RYBCTD";
				foreach (char c in solution[row][col])
					negatives = negatives.Replace(c + "", "");
				foreach (char c in negatives)
					choice.Add("-" + c);
				choice.Shuffle();
				foreach(string str in choice)
				{
					if(canBeUsed(poss[row][col], str))
					{
						clue[row][col] = str + "";
						break;
					}
				}
				clue = removeSpaces(poss, clue, row, col);
				clue = finalizeClue(clue, poss);
				clues.Add(clue);
				poss = fill(clue, poss);
				poss = remove(poss);
			}
		}
		clues = combineClues(clues);
		for (int i = 0; i < clues.Count; i++)
		{
			clues[i] = shrinkClue(clues[i]);
			Debug.LogFormat("[Shapes and Colors #{0}] Clue #{1}:", moduleId, (i + 1));
			for (int j = 0; j < NUM_ROW; j++)
			{
				string temp = "";
				for (int k = 0; k < NUM_COL; k++)
				{
					if (j >= clues[i].Length || k >= clues[i][0].Length)
						temp = temp + "KK ";
					else
					{
						if (canTurnBlack(clues[i], j, k))
							clues[i][j][k] = "KK";
						temp = temp + "" + clues[i][j][k] + " ";
					}
				}
				Debug.LogFormat("[Shapes and Colors #{0}] {1}", moduleId, temp);
			}
		}
		return clues;
	}
	//Checks if the clue being placed on that space reduces the number of possibilities to 1.
	private bool canBeUsed(List<string> possible, string clueSpace)
	{
		int sum = 0;
		if(clueSpace[0] == '-')
		{
			foreach (string poss in possible)
			{
				if (!(poss.Contains(clueSpace[1])))
					sum++;
			}
		}
		else
		{
			foreach (string poss in possible)
			{
				if (poss.Contains(clueSpace))
					sum++;
			}
		}
		return (sum == 1);
	}

	//This method loops until the clue can only be placed onto exactly 1 spot on the grid
	//1st: It checks how many places would fit the clue on the grid at its current state
	//2nd: If it is greater than 1, then the method will try to place clue facts on
	//each white space on the clue to see which space with what clue reduces it down
	//to 1 space
	//3rd: If none of the clues reduce it down to 1 spot, then the method will randomly
	//place an additional space on the clue and then loop back to the beginning of it
	//4th: Repeat steps 1 - 3 until the  amount of places you can put the clue on is 1.
	//This results in a clue that has 1 - 2 shapes on it.
	//5th: Fill the grid with white spaces in such a way that the grid becomes a 1x2, 2x1, 2x2, 1x3, 3x1, 2x3, 3x2, or 3x3
	private string[][] finalizeClue(string[][] clue, List<List<List<string>>> poss)
	{
		tryagain:
		string[][] shrink = shrinkClue(clue);
		int numPlaces = numOfPlacesToFit(shrink, poss);
		if(numPlaces > 1)
		{
			int sumWhite = 0;
			foreach(string[] r in shrink)
			{
				foreach(string c in r)
				{
					if (c.Equals("WW"))
						sumWhite++;
				}
			}
			string[][][] possibleAdd = new string[sumWhite * 6][][];
			for(int i = 0; i < possibleAdd.Length; i++)
			{
				possibleAdd[i] = new string[clue.Length][];
				for(int j = 0; j < possibleAdd[i].Length; j++)
				{
					possibleAdd[i][j] = new string[clue[j].Length];
					for(int k = 0; k < clue[j].Length; k++)
						possibleAdd[i][j][k] = clue[j][k].ToUpperInvariant();
					
				}
			}
			int cur = 0;
			for (int i = 0; i < clue.Length; i++)
			{
				for (int j = 0; j < clue[i].Length; j++)
				{
					if(possibleAdd.Length > 0 && possibleAdd[0][i][j].Equals("WW"))
					{
						string[] hints = getHintList(solution[i][j]);
						for (int k = 0; k < hints.Length; k++)
							possibleAdd[cur++][i][j] = hints[k].ToUpperInvariant();	
					}
				}
			}
			List<string[][]> choices = new List<string[][]>();
			foreach(string[][] possible in possibleAdd)
			{
				numPlaces = numOfPlacesToFit(shrinkClue(possible), poss);
				if (numPlaces == 1)
					choices.Add(possible);
			}
			if(choices.Count > 0)
				clue = choices[UnityEngine.Random.Range(0, choices.Count)];
			else
			{
				List<int[]> extraSpace = new List<int[]>();
				for(int i = 0; i < clue.Length; i++)
				{
					for(int j = 0; j < clue[i].Length; j++)
					{
						if(clue[i][j].Equals("KK"))
						{
							if (i < 2 && !(clue[i + 1][j].Equals("KK")))
								extraSpace.Add(new int[] { i, j });
							if (i > 0 && !(clue[i - 1][j].Equals("KK")))
								extraSpace.Add(new int[] { i, j });
							if (j < 2 && !(clue[i][j + 1].Equals("KK")))
								extraSpace.Add(new int[] { i, j });
							if (j > 0 && !(clue[i][j - 1].Equals("KK")))
								extraSpace.Add(new int[] { i, j });
						}
					}
				}
				int[] pos = extraSpace[UnityEngine.Random.Range(0, extraSpace.Count)];
				clue[pos[0]][pos[1]] = "WW";
			}
			goto tryagain;
		}
		int maxRow = 0, maxCol = 0, minRow = clue.Length - 1, minCol = clue[0].Length - 1;
		for(int i = 0; i < clue.Length; i++)
		{
			for(int j = 0; j < clue[i].Length; j++)
			{
				if(!(clue[i][j].Equals("KK")))
				{
					if (i > maxRow)
						maxRow = i;
					if (i < minRow)
						minRow = i;
					if (j > maxCol)
						maxCol = j;
					if (j < minCol)
						minCol = j;
				}
			}
		}
		for(int i = minRow; i <= maxRow; i++)
		{
			for(int j = minCol; j <= maxCol; j++)
			{
				if (clue[i][j].Equals("KK"))
					clue[i][j] = "WW";
			}
		}
		return clue;
	}
	//This here returns how many places the clue can fit within the current
	//grid so far. We want it to be 1 so that there is no confusion as to
	//how this piece of the clue is suppose to fit in the puzzle.
	private int numOfPlacesToFit(string[][] clue, List<List<List<string>>> poss)
	{

		int sum = 0;
		for (int i = 0; i <= (poss.Count - clue.Length); i++)
		{
			for (int j = 0; j <= (poss[i].Count - clue[i % clue.Length].Length); j++)
			{
				bool flag = true;
				for (int a = 0; a < clue.Length; a++)
				{
					for (int b = 0; b < clue[a].Length; b++)
					{
						if (!(clue[a][b].Equals("KK")) && !(clue[a][b].Equals("WW")) && getSum(poss[i + a][j + b], clue[a][b]) == 0)
						{
							flag = false;
							goto skip;
						}
					}
				}
				skip:
				if (flag)
					sum++;
			}
		}
		return sum;
	}
	//Depending what is on the space, will return additional clues to help the puzzle
	//be able to reduce the amount of spaces the clue it could be on down to 1
	private string[] getHintList(string space)
	{
		string[] hints = new string[6];
		switch (space[0])
		{
			case 'R':
				hints[0] = "R";
				hints[1] = "-Y";
				hints[2] = "-B";
				break;
			case 'Y':
				hints[0] = "-R";
				hints[1] = "Y";
				hints[2] = "-B";
				break;
			case 'B':
				hints[0] = "-R";
				hints[1] = "-Y";
				hints[2] = "B";
				break;
		}
		switch (space[1])
		{
			case 'C':
				hints[3] = "C";
				hints[4] = "-T";
				hints[5] = "-D";
				break;
			case 'T':
				hints[3] = "-C";
				hints[4] = "T";
				hints[5] = "-D";
				break;
			case 'D':
				hints[3] = "-C";
				hints[4] = "-T";
				hints[5] = "D";
				break;
		}
		return hints;
	}
	//It says fill but really it actually removes the amount of possible shapes
	//that could be placed on that space using the clue that it was given
	private List<List<List<string>>> fill(string[][] clue, List<List<List<string>>> poss)
	{
		for (int i = 0; i < clue.Length; i++)
		{
			for (int j = 0; j < clue[i].Length; j++)
			{
				if (!(clue[i][j].Equals("WW") || clue[i][j].Equals("KK")))
				{
					if (clue[i][j][0] == '-')
					{
						for (int k = 0; k < poss[i][j].Count; k++)
						{
							if (poss[i][j][k].Contains(clue[i][j][1]))
								poss[i][j].RemoveAt(k--);
						}
					}
					else
					{
						for (int k = 0; k < poss[i][j].Count; k++)
						{
							bool flag = false;
							for (int l = 0; l < clue[i][j].Length; l++)
							{
								if (!(poss[i][j][k].Contains(clue[i][j][l])))
								{
									flag = true;
									break;
								}
							}
							if (flag)
								poss[i][j].RemoveAt(k--);
						}
					}
				}
			}
		}
		return poss;
	}

	//This here removes more possible shapes/colors by using the shape/color combo
	//that it already used. Remember, there can only be one of each combination
	//Everytime it removes any possible shapes/colors, it goes back to check
	//if there are any more combos that it can remove.
	private List<List<List<string>>> remove(List<List<List<string>>> poss)
	{
		bool flag = true;
		while (flag)
		{
			List<string> remove = new List<string>();
			foreach (List<List<string>> row in poss)
			{
				foreach (List<string> col in row)
				{
					if (col.Count == 1)
						remove.Add(col[0].ToUpperInvariant());
				}
			}
			flag = false;
			for (int i = 0; i < poss.Count; i++)
			{
				for (int j = 0; j < poss[i].Count; j++)
				{
					if (poss[i][j].Count > 1)
					{
						for (int k = 0; k < poss[i][j].Count; k++)
						{
							if (remove.Contains(poss[i][j][k]))
							{
								flag = true;
								poss[i][j].RemoveAt(k--);
							}
						}
					}
				}
			}
		}
		return poss;
	}
	//Finally, after generating all the clues, any clue that has the same exact pattern in the
	//same exact spot without any shapes/colors overlapping each other will be combined.
	//This is why you might see more than 2 shapes/colors in a single clue
	private List<string[][]> combineClues(List<string[][]> clues)
	{
		List<string[][]> combine = new List<string[][]>();
		for (int i = 0; i < clues.Count; i++)
		{
			combine.Add(clues[i]);
			for (int j = i + 1; j < clues.Count; j++)
			{
				if (canCombine(combine[combine.Count - 1], clues[j]))
				{
					for (int row = 0; row < combine[combine.Count - 1].Length; row++)
					{
						for (int col = 0; col < combine[combine.Count - 1][row].Length; col++)
						{
							if (combine[combine.Count - 1][row][col].Equals("WW") || combine[combine.Count - 1][row][col].Equals("KK"))
								combine[combine.Count - 1][row][col] = clues[j][row][col];
							else if(!(clues[j][row][col].Equals("WW")))
							{
								if (clues[j][row][col][0] == '-' && clues[j][row][col][1] != combine[combine.Count - 1][row][col][1])
								{
									if (getType(combine[combine.Count - 1][row][col][1]) == 0)
										combine[combine.Count - 1][row][col] = "RYB".Replace(combine[combine.Count - 1][row][col][1] + "", "").Replace(clues[j][row][col][1] + "", "") + "W";
									else
										combine[combine.Count - 1][row][col] = "W" + "CTD".Replace(combine[combine.Count - 1][row][col][1] + "", "").Replace(clues[j][row][col][1] + "", "");
								}
								else if (clues[j][row][col].Length > combine[combine.Count - 1][row][col].Length)
									combine[combine.Count - 1][row][col] = clues[j][row][col].ToUpperInvariant();
								else if(clues[j][row][col].Length == 1 && combine[combine.Count - 1][row][col].Length == 1)
								{
									int type1 = getType(combine[combine.Count - 1][row][col][0]), type2 = getType(clues[j][row][col][0]);
									if(type1 != type2)
									{
										if (type1 == 0)
											combine[combine.Count - 1][row][col] = combine[combine.Count - 1][row][col][0] + "" + clues[j][row][col][0];
										else
											combine[combine.Count - 1][row][col] = clues[j][row][col][0] + "" + combine[combine.Count - 1][row][col][0];
									}
								}
							}
						}
					}
					clues.RemoveAt(j--);
				}
			}
		}
		return combine;
	}
	//This returns true if the 2 clues can combine without overlapping eachother.
	private bool canCombine(string[][] c1, string[][] c2)
	{
		for (int i = 0; i < c1.Length; i++)
		{
			for (int j = 0; j < c1[i].Length; j++)
			{
				if (c1[i][j].Equals("KK") && !(c2[i][j].Equals("KK")))
					return false;
				if (c2[i][j].Equals("KK") && !(c1[i][j].Equals("KK")))
					return false;
				if (!(c1[i][j].Equals("KK")) && !(c1[i][j].Equals("WW")) && !(c2[i][j].Equals("KK")) && !(c2[i][j].Equals("WW")))
				{
					if((c1[i][j][0] == '-' && c2[i][j][0] != '-') || (c1[i][j][0] != '-' && c2[i][j][0] == '-'))
						return false;
					if (c1[i][j][0] == '-' && c2[i][j][0] == '-')
					{
						if(getType(c1[i][j][1]) != getType(c2[i][j][1]))
							return false;
					}
				}
			}
		}
		return true;
	}
	//Only used to check if the types are the same type so they could combine
	private int getType(char type)
	{
		switch(type)
		{
			case 'R': case 'Y': case 'B':
				return 0;
			default:
				return 1;
		}
	}
	//This shrinks the clue, removing any unnessecary black spaces to move the clue
	//up to the upper left corner so that the user doesn't know which position the
	//clue was generated from. It also helps with determining if the clue requires
	//more shapes, colors, or spaces for it to be unique.
	private string[][] shrinkClue(string[][] clue)
	{
		string[][] shrink = new string[clue.Length][];
		for(int i = 0; i < clue.Length; i++)
		{
			shrink[i] = new string[clue[i].Length];
			for (int j = 0; j < clue.Length; j++)
				shrink[i][j] = clue[i][j].ToUpperInvariant();
		}
		//Removing bottom row
		bottomRow:
		string str = "";
		for (int i = 0; i < shrink[shrink.Length - 1].Length; i++)
			str += shrink[shrink.Length - 1][i];
		bool v = canRemove(str);
		if(v)
		{
			string[][] temp = new string[shrink.Length - 1][];
			for(int i = 0; i < temp.Length; i++)
			{
				temp[i] = new string[shrink[i].Length];
				for (int j = 0; j < temp[i].Length; j++)
					temp[i][j] = shrink[i][j].ToUpperInvariant();
			}
			shrink = temp;
			goto bottomRow;
		}
		//Removing top row
		topRow:
		str = "";
		for (int i = 0; i < shrink[0].Length; i++)
			str += shrink[0][i];
		v = canRemove(str);
		if(v)
		{
			string[][] temp = new string[shrink.Length - 1][];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = new string[shrink[i + 1].Length];
				for (int j = 0; j < temp[i].Length; j++)
					temp[i][j] = shrink[i + 1][j].ToUpperInvariant();
			}
			shrink = temp;
			goto topRow;
		}
		//Removing right column
		rightCol:
		str = "";
		for (int i = 0; i < shrink.Length; i++)
			str += shrink[i][shrink[i].Length - 1];
		v = canRemove(str);
		while (v)
		{
			string[][] temp = new string[shrink.Length][];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = new string[shrink[i].Length - 1];
				for (int j = 0; j < temp[i].Length; j++)
					temp[i][j] = shrink[i][j].ToUpperInvariant();
			}
			shrink = temp;
			goto rightCol;
		}
		//Removing LEFT column
		leftCol:
		str = "";
		for (int i = 0; i < shrink.Length; i++)
			str += shrink[i][0];
		v = canRemove(str);
		while (v)
		{
			string[][] temp = new string[shrink.Length][];
			for (int i = 0; i < temp.Length; i++)
			{
				temp[i] = new string[shrink[i].Length - 1];
				for (int j = 0; j < temp[i].Length; j++)
					temp[i][j] = shrink[i][j + 1].ToUpperInvariant();
			}
			shrink = temp;
			goto leftCol;
		}
		return shrink;
	}
	//This returns true if all the spaces for that section is black
	private bool canRemove(string str)
	{
		foreach(char c in str)
		{
			if (c != 'K')
				return false;
		}
		return true;
	}
	//This turns the spaces black if the space next to it is already filled
	private string[][] removeSpaces(List<List<List<string>>> poss, string[][] clue, int row, int col)
	{
		if(row > 0 && poss[row - 1][col].Count == 1)
		{
			for(int i = 0; i < clue[0].Length; i++)
				clue[0][i] = "KK";
			if (row > 1 && poss[row - 2][col].Count == 1)
			{
				for (int i = 0; i < clue[1].Length; i++)
					clue[1][i] = "KK";
			}
		}
		if (row < 2 && poss[row + 1][col].Count == 1)
		{
			for (int i = 0; i < clue[2].Length; i++)
				clue[2][i] = "KK";
			if (row < 1 && poss[row + 2][col].Count == 1)
			{
				for (int i = 0; i < clue[1].Length; i++)
					clue[1][i] = "KK";
			}
		}
		if(col > 0 && poss[row][col - 1].Count == 1)
		{
			for (int i = 0; i < clue.Length; i++)
				clue[i][0] = "KK";
			if (col > 1 && poss[row][col - 2].Count == 1)
			{
				for (int i = 0; i < clue.Length; i++)
					clue[i][1] = "KK";
			}
		}
		if (col < 2 && poss[row][col + 1].Count == 1)
		{
			for (int i = 0; i < clue.Length; i++)
				clue[i][2] = "KK";
			if (col < 1 && poss[row][col + 2].Count == 1)
			{
				for (int i = 0; i < clue.Length; i++)
					clue[i][1] = "KK";
			}
		}
		return clue;
	}
	private int getSum(List<string> poss, string type)
	{
		int sum = 0;
		if(type[0] == '-')
		{
			foreach (string str in poss)
			{
				if (!(str.Contains(type[1])))
					sum++;
			}
		}
		else
		{
			foreach (string str in poss)
			{
				if (str.Contains(type))
					sum++;
			}
		}
		return sum;
	}
	//This checks if the space can be turned black.
	//If it can, it returns a random boolean state.
	private bool canTurnBlack(string[][] clue, int row, int col)
	{
		if(clue[row][col].Equals("WW"))
		{
			string[][] temp = new string[NUM_ROW][];
			for(int i = 0; i < NUM_ROW; i++)
			{
				temp[i] = new string[NUM_COL];
				for(int j = 0; j < NUM_COL; j++)
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
				for(int i = 0; i < temp.Length; i++)
				{
					for(int j = 0; j < temp[i].Length; j++)
					{
						if(!(temp[i][j].Equals("KK")))
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
				while(true)
				{
					if(row > 0 && !(temp[row - 1][col].Equals("KK")))
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
						switch(dir[dir.Length - 1])
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
				for(int i = 0; i < temp.Length; i++)
				{
					for(int j = 0; j < temp[i].Length; j++)
					{
						if (!(temp[i][j].Equals("KK")))
							return false;
					}
				}
				return (UnityEngine.Random.Range(0, 2) == 0);
			}
		}
		return false;
	}
	private void displayClue()
	{
		for (int i = 0; i < clues[clueCursor].Length; i++)
			clueMeshRender[i].material = clues[clueCursor][i];
	}
	private int mod(int n, int m)
	{
		while (n < 0)
			n += m;
		return (n % m);
	}
	private int getMat(string space)
	{
		switch(space)
		{
			case "WW": return 0;
			case "KK": return 1;
			case "C": case "WC": return 2;
			case "T": case "WT": return 3;
			case "D": case "WD": return 4;
			case "R": case "RW": return 5;
			case "Y": case "YW": return 6;
			case "B": case "BW": return 7;
			case "-C": return 8;
			case "-T": return 9;
			case "-D": return 10;
			case "-R": return 11;
			case "-Y": return 12;
			case "-B": return 13;
			case "RC": return 14;
			case "RT": return 15;
			case "RD": return 16;
			case "YC": return 17;
			case "YT": return 18;
			case "YD": return 19;
			case "BC": return 20;
			case "BT": return 21;
			case "BD": return 22;
		}
		return -1;
	}
	private bool doesFit(string space, string clue)
	{
		if (clue[0] == '-')
			return !(space.Contains(clue[1]));
		else if (clue.Equals(space))
			return true;
		return (space.Contains(clue));
	}
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press|p up down (R)ed (Y)ellow (B)lue (C)ircle (T)riangle (D)iamond TL/1 TM/2 TR/3 ML/4 MM/5 MR/6 BL/7 BM/8 BR/9 to press those buttons on the module. !{0} submit to submit your current grid. !{0} clear to clear the entire grid.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] param = command.ToUpper().Split(' ');
		if ((Regex.IsMatch(param[0], @"^\s*PRESS\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(param[0], @"^\s*P\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) && param.Length > 1)
		{
			if (isButton(param))
			{
				yield return null;
				for (int i = 1; i < param.Length; i++)
				{
					switch (param[i])
					{
						case "UP":
							ClueUp.OnInteract();
							break;
						case "DOWN":
							ClueDown.OnInteract();
							break;
						case "RED": case "R":
							colorInput[0].OnInteract();
							break;
						case "YELLOW": case "Y":
							colorInput[1].OnInteract();
							break;
						case "BLUE": case "B":
							colorInput[2].OnInteract();
							break;
						case "CIRCLE": case "C":
							shapeInput[0].OnInteract();
							break;
						case "TRIANGLE": case "T":
							shapeInput[1].OnInteract();
							break;
						case "DIAMOND": case "D":
							shapeInput[2].OnInteract();
							break;
						case "TL": case "1":
							grid[0].OnInteract();
							break;
						case "TM": case "2":
							grid[1].OnInteract();
							break;
						case "TR": case "3":
							grid[2].OnInteract();
							break;
						case "ML": case "4":
							grid[3].OnInteract();
							break;
						case "MM": case "5":
							grid[4].OnInteract();
							break;
						case "MR": case "6":
							grid[5].OnInteract();
							break;
						case "BL": case "7":
							grid[6].OnInteract();
							break;
						case "BM": case "8":
							grid[7].OnInteract();
							break;
						case "BR": case "9":
							grid[8].OnInteract();
							break;
					}
					yield return new WaitForSeconds(0.2f);
				}
			}
			else
				yield return "sendtochat An error occured because the user inputted something wrong.";
		}
		else if (Regex.IsMatch(param[0], @"^\s*SUBMIT\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && param.Length == 1)
		{
			yield return null;
			submit.OnInteract();
		}
		else if (Regex.IsMatch(param[0], @"^\s*CLEAR\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && param.Length == 1)
		{
			yield return null;
			if(colorCursor >= 0)
			{
				colorInput[colorCursor].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			if (shapeCursor >= 0)
			{
				shapeInput[shapeCursor].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			foreach(KMSelectable space in grid)
			{
				space.OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
		}
		else
			yield return "sendtochat An error occured because the user inputted something wrong.";
	}
	private bool isButton(string[] param)
	{
		for(int i = 1; i < param.Length; i++)
		{
			switch(param[i])
			{
				case "UP": case "DOWN":
				case "RED":		case "R":
				case "YELLOW":	case "Y":
				case "BLUE":	case "B":
				case "CIRCLE":		case "C":
				case "TRIANGLE":	case "T":
				case "DIAMOND":		case "D":
				case "TL":	case "1":
				case "TM":	case "2":
				case "TR":	case "3":
				case "ML":	case "4":
				case "MM":	case "5":
				case "MR":	case "6":
				case "BL":	case "7":
				case "BM":	case "8":
				case "BR":	case "9":
					break;
				default:
					return false;
			}
		}
		return true;
	}
	
}
