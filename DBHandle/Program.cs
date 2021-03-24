

using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DBHandle
{
	/**
	 * Get all the files child'd to the filepath
	 * Grab each file and parse it's contents
	 * Take the contents and turn them into objects
	 * 
	 * Location
	 * Catagorey
	 * Food Item
	 * ->Within Food Item list Calories, Serving Size (Obj), 
	 */

	class Program
	{
		static void Main(string[] args)
		{
			Set_Up SE = new Set_Up();
			SE.Generate_Contents();
		}
	}

	class Set_Up
	{
		string Filepath = "D:\\Documents\\TTU_Nutrition_Facts";
		List<Dining_Location> TTU_Meal_Data = new List<Dining_Location>();
		public void Generate_Contents()
		{

			string[] PDFS = Directory.GetFiles(Filepath);
			//PDFS.Length
			for (int a = 0; a < PDFS.Length; a++)
			{
				TTU_Meal_Data.Add(Create_Obj(PDFS[a]));
				//Console.WriteLine("-----------------------------\n");
			}

		}

		public string ExtractTextFromPdf(string path)
		{
			using (PdfReader reader = new PdfReader(path))
			{
				StringBuilder text = new StringBuilder();

				for (int i = 1; i <= reader.NumberOfPages; i++)
				{
					text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
				}

				return text.ToString();
			}
		}

		private Dining_Location Create_Obj(string PDFN)
		{

			string RawD = ExtractTextFromPdf(PDFN);
			//Console.WriteLine(RawD);
			string[] Lines = Regex.Split(RawD, "\r\n|\r|\n");

			//Create Header
			string Header = FormatHeader(PDFN);
			//Console.WriteLine(Header);

			Dining_Location DL = new Dining_Location(Header);

			//Get Data and send it to the Food Object
			List<string> Formatted_Sheet = Format_Sheet(Lines);
			string CurrentLabel = "Offered Food";
			foreach (string L in Formatted_Sheet)
			{
				//Test to see if L is correctly formatted for food item
				string newS = L.Replace("<", "");
				newS = newS.Replace(">", "");
				newS = Regex.Replace(newS, "[\\d*]", "@");
				//Console.WriteLine(newS + "\n");
				string[] AllP = newS.Split(" @");
				if (L.Contains("| LABEL"))
				{
					CurrentLabel = L.Replace("| LABEL", "").Replace("Serving Size", "").Replace("180 2 24 1 4  Wheat, Milk, Soy", "")
						.Replace("Serving Size Calories Fat (g) Carbs (g) Fiber Protein (g) Vegan Vegetarian Gluten Free Allergen/Contains", "")
						.Replace("<", "").Replace("Nuts Pecans/Walnuts", "").Replace("", "").Replace("Wheat, Milk, Soy", "").Replace("?", "");
					CurrentLabel = Regex.Replace(CurrentLabel, "[\\d*]", "");


				}
				else if (AllP.Length == 7)
				{
					//Add to the list of food in the Dining Location
					Food NewF = new Food(Header, CurrentLabel, L);
					DL.Add_Food(NewF);
				}

				//Console.WriteLine(L + "\n");
			}

			return DL;
		}

		private List<string> Format_Sheet(string[] L)
		{
			List<string> FR = new List<string>();

			for (int a = 0; a < L.Length; a++)
			{
				bool isDel = DeleteLine(L[a]);
				if (!isDel)
				{
					bool isLab = isLabel(L[a]);
					if (isLab)
					{
						L[a] = L[a].Replace(":", "");
						string[] GetLab = L[a].Split("Serving Size");
						if (GetLab.Length > 1)
						{
							string newL = GetLab[0] + " | LABEL";
							FR.Add(newL);
						}
						else
						{
							string newL = L[a] + " | LABEL";
							FR.Add(newL);
						}

					}
					else
					{
						FR.Add(L[a]);
					}
				}
			}

			//Special Case Fix
			if (FR.Count > 1)
			{
				for (int i = 0; i < FR.Count - 1; i++)
				{
					if (!String.IsNullOrWhiteSpace(FR[i]))
					{
						string CS = FR[i];
						CS = Regex.Replace(CS, @"\s+", "");

						if (char.IsNumber(CS[0]) || CS[0] == '.')
						{
							//Combine L2 with L1

							string L2 = FR[i + 1];
							string L1 = FR[i];
							FR[i] = L2 + " " + L1;
							FR.RemoveAt(i + 1);
							i--;
						}
					}
					else
					{
						FR.RemoveAt(i);
						i--;
					}

				}

			}

			return FR;
		}


		private bool isLabel(string L)
		{
			L = L.ToLower();
			if (L.Contains("") || L.Contains(","))
			{
				return false;
			}
			string newS = Regex.Replace(L, "[\\d*]", "@");
			if (L.Contains("fiber") || L.Contains("protein") || L.Contains("serving size") || L.Contains(":"))
			{
				return true;
			}
			else if (!newS.Contains("@"))
			{
				return true;
			}
			return false;
		}

		private bool DeleteLine(string L)
		{
			L = L.ToLower();
			//Console.WriteLine(L);

			if (L.Contains("updated") || L.Contains("fall") || L.Contains("2020") || L.Contains("location"))
			{
				return true;
			}
			else if (L.Contains("yes =") || L.Contains("yellow = needs") || L.Contains("indicates a") ||
				L.Contains("may contain") || L.Contains("food allergies.") ||
				L.Contains("indicates smart choice item") || L.Contains("yes") || L.Contains("been fried") ||
				L.Contains("allergy/contains") || L.Contains("food allergies.") || L.Contains("indicates a"))
			{
				return true;
			}
			return false;
		}

		private string FormatHeader(string RawH)
		{
			StringBuilder FS = new StringBuilder();
			string Header = RawH.Replace("Fall", "").Replace("2020", "").Replace("-", "").Replace(".pdf", "").Replace(Filepath + "\\", "");

			//Skip over the first value
			FS.Append(Header[0]);
			for (int a = 1; a < Header.Length; a++)
			{
				if (char.IsUpper(Header[a]))
				{
					FS.Append(" ");
				}

				FS.Append(Header[a]);
			}

			return FS.ToString();
		}

	}

	class Dining_Location
	{
		private string Name;
		private List<Food> AllFood = new List<Food>();
		//Use Dictionary to hold Food Items, Key would be type of food, Value would be Food Object
		public Dining_Location(string N)
		{
			Name = N;
		}

		public void Add_Food(Food Item)
		{
			AllFood.Add(Item);
		}
	}

	class Food
	{

		private string Name;
		private string Allergy_Contents;
		private string Dining_Place;
		private string Type_Of_Food;

		private string ServingS;
		private int Amount;

		private float Calories;
		private float Fat;
		private float Carbs;
		private float Fiber;
		private float Protein;

		private bool VeganFriendly;

		public Food(string DL, string Label, string RawD)
		{
			//Format the string, parse data, ect
			string newS = RawD.Replace("<", "").Replace(">", "");
			int index = 0;
			int lastI = 0;
			for (int a = 0; a < newS.Length - 1; a++)
			{
				char Part = newS[a];
				char nextPart = newS[a + 1];
				if (Part.Equals(' ') && char.IsNumber(nextPart) && index < 7)
				{
					string Data = newS.Substring(lastI, a - lastI);
					SetConstruct(index, Data);
					index++;
					lastI = a;
				}
			}

			string D = newS.Substring(lastI);
			SetConstruct(index, D);

			Type_Of_Food = Label;
			Dining_Place = DL;

			//Print_All();
		}

		private void SetConstruct(int ind, string Data)
		{
			if (ind == 0)
			{
				Name = Data;
			}
			else if (ind == 1)
			{
				ServingS = Data;
				Amount = 0;
			}
			else if (ind == 2)
			{
				//Console.WriteLine(Data);
				Data = Regex.Replace(Data, "[^0-9.]", "");
				Calories = float.Parse(Data, CultureInfo.InvariantCulture.NumberFormat);
			}
			else if (ind == 3)
			{
				Data = Regex.Replace(Data, "[^0-9.]", "");
				Fat = float.Parse(Data, CultureInfo.InvariantCulture.NumberFormat);
			}
			else if (ind == 4)
			{
				Data = Regex.Replace(Data, "[^0-9.]", "");
				Carbs = float.Parse(Data, CultureInfo.InvariantCulture.NumberFormat);
			}
			else if (ind == 5)
			{
				Data = Regex.Replace(Data, "[^0-9.]", "");
				Fiber = float.Parse(Data, CultureInfo.InvariantCulture.NumberFormat);
			}
			else if (ind == 6)
			{
				string VF = Regex.Replace(Data, "[^0-9.]", "");
				Protein = float.Parse(VF, CultureInfo.InvariantCulture.NumberFormat);

				string V7 = Regex.Replace(Data, "[\\d *]", "");
				//Console.WriteLine(V7);
				if (V7.Contains("") || V7.Contains("?"))
				{
					VeganFriendly = true;
				}
				else
				{
					VeganFriendly = false;
				}

				Allergy_Contents = V7.Replace("", "").Replace(" ", "").Replace("?", "");
			}
		}
		//Handle Serving Size increase/decreases

		public void Increase_Serving()
		{
			Amount++;
		}

		public void Decrease_Serving()
		{
			Amount--;
		}

		public string get_Type()
		{
			return Type_Of_Food;
		}

		public float get_Calories()
		{
			return Calories;
		}

		public float get_Fat()
		{
			return Fat;
		}

		public float get_Carbs()
		{
			return Carbs;
		}

		public float get_Fiber()
		{
			return Fiber;
		}

		public float get_Protein()
		{
			return Protein;
		}

		public int get_ServingAmount()
		{
			return Amount;
		}


		public void Print_All()
		{
			string D = Dining_Place + " | " + Type_Of_Food + " | " + Name + " | " + ServingS + " | " + Calories + " | " + Fat + " | " + Carbs
				+ " | " + Fiber + " | " + Protein + " | " + VeganFriendly + " | " + Allergy_Contents;

			Console.WriteLine(D + "\n");

		}

	}

}


