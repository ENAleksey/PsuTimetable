using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net.Http;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace PsuTimetable
{
	public partial class App : Application
	{
		public static HttpClient MainClient { get; set; }
		public static bool IsSignedIn { get; set; }

		public App()
		{
			InitializeComponent();

			MainClient = new HttpClient
			{
				BaseAddress = new Uri("https://student.psu.ru/pls/stu_cus_et/")
			};

			if (Credentials.IsSaved())
			{
				MainPage = new NavigationPage(new MainTabbedPage());
			}
			else
			{
				MainPage = new NavigationPage(new LoginPage());
			}

		}

		public static async Task<int> SendLoginRequest(string username, string password)
		{
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				return 1;
			}

			string postData = "p_redirect=&p_username=" + HttpUtility.UrlEncode(username, Encoding.GetEncoding(1251)) + "&p_password=" + password;
			var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

			HttpResponseMessage response = await MainClient.PostAsync("stu.login", content);

			if (!response.IsSuccessStatusCode)
			{
				return 2;
			}

			string html = await response.Content.ReadAsStringAsync();

			if (html.Contains("Неверное имя пользователя или пароль"))
			{
				return 3;
			}
			else if (html.Contains("Превышен лимит"))
			{
				return 4;
			}

			return 0;
		}

		public static async Task<bool> IsConnectionAvailable()
		{
			var req = WebRequest.Create("http://www.google.com");
			req.Timeout = 5000;
			try
			{
				await req.GetResponseAsync();
				return true;
			}
			catch (WebException)
			{
				return false;
			}
		}

		protected override void OnStart()
		{
			// Handle when your app starts
		}

		protected override void OnSleep()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume()
		{
			// Handle when your app resumes
		}
	}
}
