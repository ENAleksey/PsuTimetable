using System;
using System.Collections.Generic;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;

namespace PsuTimetable
{
	public class Pair
	{
		public bool IsExist { get; set; }
		public string Name { get; set; }
		public string Number { get; set; }
		public string StartTime { get; set; }
		public string TeacherName { get; set; }
		public string Classroom { get; set; }
	}

	public class Day
	{
		public bool ContainPairs { get; set; }
		public string Name { get; set; }
		public List<Pair> Pairs { get; set; }
		public Day() => Pairs = new List<Pair>();
	}

	public class Week
	{
		public int Number { get; set; }
		public string Name { get; set; }
		public List<Day> Days { get; set; }
		public Week() => Days = new List<Day>();
	}

	public class TimetableData
	{
		public int CurrentWeekId { get; set; }
		public List<Week> Weeks { get; set; }
		public DateTime LastUpdateTime { get; set; }

		public TimetableData() => Weeks = new List<Week>();
	}

	public static class Timetable
	{
		private static TimetableData timetableData = new TimetableData();
		private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "timetable.xml");

		public static List<Week> GetWeeks()
		{
			return timetableData.Weeks;
		}

		public static int GetCurrentWeekId()
		{
			int nextWeekId = timetableData.CurrentWeekId + 1;
			return (DateTime.Now.DayOfWeek == DayOfWeek.Sunday && nextWeekId < timetableData.Weeks.Count) ? nextWeekId : timetableData.CurrentWeekId;
		}

		public static DateTime GetLastUpdate()
		{
			return timetableData.LastUpdateTime;
		}

		private static bool AreDifferentWeeks(DateTime date1, DateTime date2)
		{
			var d1 = date1.AddDays(-(((int)date1.DayOfWeek + 6) % 7));
			var d2 = date2.AddDays(-(((int)date2.DayOfWeek + 6) % 7));

			return d1.Date != d2.Date;
		}

		public static bool NeedUpdate()
		{
			return !File.Exists(filePath) || (DateTime.Now.DayOfWeek != DayOfWeek.Sunday && AreDifferentWeeks(DateTime.Now, timetableData.LastUpdateTime));
		}

		public static void Save()
		{
			using (var writer = File.Open(filePath, FileMode.Create, FileAccess.Write))
			{
				var serializer = new XmlSerializer(typeof(TimetableData));
				serializer.Serialize(writer, timetableData);
			}
		}

		public static bool Load()
		{
			if (File.Exists(filePath))
			{
				using (var reader = new StreamReader(filePath))
				{
					var serializer = new XmlSerializer(typeof(TimetableData));
					timetableData = (TimetableData)serializer.Deserialize(reader);

					return true;
				}
			}

			return false;
		}

		public static void Clear()
		{
			File.Delete(filePath);
		}

		public static async Task Update()
		{
			HttpResponseMessage response = await App.MainClient.GetAsync("stu.timetable");
			string html = await response.Content.ReadAsStringAsync();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);

			int startWeekNumber = -1;
			HtmlNode weeksNode = htmlDoc.DocumentNode.SelectSingleNode("//html/body/div[2]/div/div[2]/div[2]/ul");
			timetableData.Weeks.Clear();

			foreach (HtmlNode weekNode in weeksNode.SelectNodes("./li"))
			{
				Week week;

				HtmlNode refNode = weekNode.SelectSingleNode("./a");
				if (refNode != null)
				{
					int weekNumber = int.Parse(refNode.InnerText);
					if (startWeekNumber == -1)
						startWeekNumber = weekNumber;

					week = await InitWeek(weekNumber);
				}
				else
				{
					int weekNumber = int.Parse(weekNode.InnerText);
					if (startWeekNumber == -1)
						startWeekNumber = weekNumber;

					week = await InitWeek(weekNumber);
					timetableData.CurrentWeekId = weekNumber - startWeekNumber;
				}

				timetableData.Weeks.Add(week);
			}

			timetableData.LastUpdateTime = DateTime.Now;
		}

		private static async Task<Week> InitWeek(int weekNumber)
		{
			HttpResponseMessage response = await App.MainClient.GetAsync("stu.timetable?p_cons=n&p_week=" + weekNumber.ToString());
			string html = await response.Content.ReadAsStringAsync();

			var htmlDoc = new HtmlDocument();
			htmlDoc.LoadHtml(html);

			Week week = new Week
			{
				Number = weekNumber
			};

			HtmlNode weekNameNode = htmlDoc.DocumentNode.SelectSingleNode("//html/body/div[2]/div/div[2]/div[2]/div[2]/span");
			HtmlNode timetableNode = htmlDoc.DocumentNode.SelectSingleNode("//html/body/div[2]/div/div[2]/div[3]");

			if (weekNameNode != null)
			{
				week.Name = weekNameNode.InnerText.TrimEnd('\n');
			}

			if (timetableNode != null)
			{
				foreach (HtmlNode dayNode in timetableNode.SelectNodes("./div"))
				{
					Day day = new Day();

					HtmlNode tableNode = dayNode.SelectSingleNode("./table");
					day.ContainPairs = (tableNode != null);
					day.Name = dayNode.SelectSingleNode("./h3").InnerText;

					if (tableNode != null)
					{
						foreach (HtmlNode pairNode in tableNode.SelectNodes("./tr"))
						{
							Pair pair = new Pair();

							HtmlNode pairInfoNode = pairNode.SelectSingleNode("./td[2]/div");
							pair.IsExist = (pairInfoNode != null);
							pair.StartTime = pairNode.SelectSingleNode("./td[1]/font").InnerText;
							pair.Number = pairNode.SelectSingleNode("./td[1]").InnerText.Substring(0, 6);

							if (pairInfoNode != null)
							{
								pair.Name = pairInfoNode.SelectSingleNode("./div[1]/span[2]").InnerText.Trim('\n');
								pair.TeacherName = pairInfoNode.SelectSingleNode("./div[1]/span[1]/a[1]").InnerText;
								pair.Classroom = pairInfoNode.SelectSingleNode("./div[2]/span").InnerText;
							}

							day.Pairs.Add(pair);
						}
					}

					week.Days.Add(day);
				}
			}

			return week;
		}
	}
}