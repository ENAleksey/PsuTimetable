using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Net.Http;
using System.Web;
using System.IO;

namespace PsuTimetable
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LoginPage : ContentPage
	{
		private readonly Label messageLabel;
		private readonly Entry usernameEntry;
		private readonly Entry passwordEntry;
		private readonly Switch saveCredentialsSwitch;

		public LoginPage()
		{
			InitializeComponent();

			Padding = new Thickness(10, 20, 10, 10);

			messageLabel = new Label
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				HorizontalTextAlignment = TextAlignment.Center,
				TextColor = Color.OrangeRed
			};

			usernameEntry = new Entry
			{
				Placeholder = "Имя пользователя",
				ReturnType = ReturnType.Next,
				TextColor = Color.Black,
				PlaceholderColor = Color.Gray
			};

			passwordEntry = new Entry
			{
				Placeholder = "Пароль",
				IsPassword = true,
				ReturnType = ReturnType.Done,
				TextColor = Color.Black,
				PlaceholderColor = Color.Gray
			};

			var loginButton = new Button
			{
				Text = "Войти",
				HorizontalOptions = LayoutOptions.FillAndExpand,
				TranslationY = 20,
				BackgroundColor = Color.Accent,
				TextColor = Color.White,
				CornerRadius = 50,
				FontSize = 16,
				HeightRequest = 40
			};

			var openInBrowserButton = new Button()
			{
				Text = "Открыть сайт в браузере",
				CornerRadius = 50,
				FontSize = 14,
				TextColor = Color.Accent,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.EndAndExpand,
				HeightRequest = 30,
				MinimumHeightRequest = 30,
				BackgroundColor = Color.White
			};

			saveCredentialsSwitch = new Switch
			{
				IsToggled = true
			};

			usernameEntry.Completed += (object sender, EventArgs args) => passwordEntry.Focus();
			passwordEntry.Completed += async (object sender, EventArgs args) => await Login(usernameEntry.Text, passwordEntry.Text, saveCredentialsSwitch.IsToggled);
			loginButton.Clicked += async (object sender, EventArgs args) => await Login(usernameEntry.Text, passwordEntry.Text, saveCredentialsSwitch.IsToggled);
			openInBrowserButton.Clicked += (object sender, EventArgs args) => Device.OpenUri(new Uri("https://student.psu.ru/pls/stu_cus_et/stu.timetable"));

			Content = new StackLayout
			{
				VerticalOptions = LayoutOptions.FillAndExpand,
				Children = {
					messageLabel,
					usernameEntry,
					passwordEntry,
					new StackLayout {
						Orientation = StackOrientation.Horizontal,
						HorizontalOptions = LayoutOptions.End,
						Children = {
							new Label {
								Text = "Запомнить меня",
								VerticalTextAlignment = TextAlignment.Center,
								TextColor = Color.Black
							},
							saveCredentialsSwitch
						}
					},
					loginButton,
					openInBrowserButton
				}
			};
		}

		private async Task Login(string username, string password, bool bSaveCredentials)
		{
			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
			{
				messageLabel.Text = "Введите имя пользователя и пароль";
				return;
			}

			if (!await App.IsConnectionAvailable())
			{
				messageLabel.Text = "Не удалось подключиться к сети";
				return;
			}

			string postData = "p_redirect=&p_username=" + HttpUtility.UrlEncode(username, Encoding.GetEncoding(1251)) + "&p_password=" + password;
			var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");

			HttpResponseMessage response = await App.MainClient.PostAsync("stu.login", content);

			if (!response.IsSuccessStatusCode)
			{
				messageLabel.Text = "Не удалось получить доступ к сайту (" + response.StatusCode.ToString() + ")";
				return;
			}

			string html = await response.Content.ReadAsStringAsync();

			// //*[@id="form"]/div[2]/div[1]/div

			if (html.Contains("Неверное имя пользователя или пароль"))
			{
				messageLabel.Text = "Неверное имя пользователя или пароль";
				passwordEntry.Text = string.Empty;
				passwordEntry.Focus();
				return;
			}
			else if (html.Contains("Превышен лимит"))
			{
				messageLabel.Text = "Превышен лимит (5) неудачных попыток ввода пароля. Повторите попытку через 10 минут";
				return;
			}

			if (bSaveCredentials && !Credentials.IsSaved())
			{
				await Credentials.Save(username, password);
			}

			App.IsSignedIn = true;

			Navigation.InsertPageBefore(new MainTabbedPage(), this);
			await Navigation.PopAsync();
		}
	}
}