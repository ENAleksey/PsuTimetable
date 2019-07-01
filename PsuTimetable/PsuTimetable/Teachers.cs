using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Xamarin.Forms;

namespace PsuTimetable
{
    public class Teacher
    {
        public string ImageUri { get; set; }
        public string Name { get; set; }
        public string Chair { get; set; }
        public string Description { get; set; }
    }

    public static class Teachers
    {
        private static List<Teacher> teachers = new List<Teacher>();
        private static readonly string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "teachers.xml");
        private static readonly Uri baseUri = new Uri("https://student.psu.ru/pls/stu_cus_et/stu.teachers");

        public static List<Teacher> GetTeachers()
        {
            return teachers;
        }

        public static void Save()
        {
			using (var writer = File.Open(filePath, FileMode.Create, FileAccess.Write))
			{
				var serializer = new XmlSerializer(typeof(List<Teacher>));
                serializer.Serialize(writer, teachers);
			}
        }

        public static bool Load()
        {
            if (File.Exists(filePath))
            {
				using (var reader = new StreamReader(filePath))
				{
                    var serializer = new XmlSerializer(typeof(List<Teacher>));
                    teachers = (List<Teacher>)serializer.Deserialize(reader);

					return teachers.Count != 0;
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
            HttpResponseMessage response = await App.MainClient.GetAsync("stu.teachers");
            string html = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            HtmlNode teachersNode = htmlDoc.DocumentNode.SelectSingleNode("//html/body/div[2]/div/div[2]");

            if (teachersNode == null)
                return;

			teachers.Clear();

			foreach (HtmlNode teacherInfoNode in teachersNode.SelectNodes("./table"))
            {
                Teacher teacher = new Teacher
                {
					ImageUri = new Uri(baseUri, teacherInfoNode.SelectSingleNode("./tr/td[1]/div/img").Attributes["src"].Value).AbsoluteUri,
					Name = teacherInfoNode.SelectSingleNode("./tr/td[2]/div[1]").InnerText.Trim('\n', ' '),
                    Chair = teacherInfoNode.SelectSingleNode("./tr/td[2]/div[2]").InnerText.Trim('\n', ' '),
                    Description = teacherInfoNode.SelectSingleNode("./tr/td[2]/div[3]").InnerText.Trim('\n', ' ')
                };

                teachers.Add(teacher);
            }
        }
    }
}