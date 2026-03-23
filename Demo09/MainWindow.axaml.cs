using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using Demo09.Context;
using Demo09.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Timers;

namespace Demo09
{
    public partial class MainWindow : Window
    {
        private string _caphaText;
        private int _countLogin;
        private Timer _blockTimer;
        private bool _isBlocked;

        public MainWindow()
        {
            _countLogin = 0;
            _isBlocked = false;
            InitializeComponent();
            GenerateCaptcha();
        }

        private async void ButtonLogin(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Проверка блокировки
            if (_isBlocked)
            {
                await MessageBoxManager.GetMessageBoxStandard("Блокировка", "Подождите 10 секунд перед следующей попыткой", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                return;
            }

            // Проверка полей
            if (string.IsNullOrWhiteSpace(LoginBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Text))
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Заполните логин и пароль", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                return;
            }

            // Проверка капчи (если видна)
            if (StackPanelCapha.IsVisible)
            {
                if (string.IsNullOrWhiteSpace(CaphaText.Text))
                {
                    await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Введите текст с капчи", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                    return;
                }

                if (CaphaText.Text != _caphaText)
                {
                    // НЕВЕРНАЯ КАПЧА — БЛОКИРОВКА НА 10 СЕКУНД
                    await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Неверный код капчи. Доступ заблокирован на 10 секунд", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                    CaphaText.Text = "";
                    GenerateCaptcha();
                    StartBlockTimer();
                    return;
                }
            }

            // Проверка пользователя в БД
            try
            {
                using var context = new PostgresContext();
                var user = context.Users.Include(u => u.Role).FirstOrDefault(u => u.Login == LoginBox.Text && u.Password == PasswordBox.Text);

                if (user != null)
                {
                    await MessageBoxManager.GetMessageBoxStandard("Успех", "Добро пожаловать!", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                    Application.Current.Resources["CurrentUser"] = user;

                    var windowMenu = new WindowMenu();
                    windowMenu.Show();
                    Close();
                }
                else
                {
                    _countLogin++;

                    if (_countLogin >= 4)
                    {
                        StackPanelCapha.IsVisible = true;
                        GenerateCaptcha();
                        await MessageBoxManager.GetMessageBoxStandard("Внимание", "Введите код с картинки", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                    }
                    else
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Ошибка", "Неверный логин или пароль", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Ошибка подключения: {ex.Message}", ButtonEnum.Ok).ShowWindowDialogAsync(this);
            }
        }

        private void StartBlockTimer()
        {
            _isBlocked = true;
            MainPanel.IsEnabled = false;

            int remainingSeconds = 10;
            TimerText.Text = $"Блокировка: {remainingSeconds} сек";
            TimerText.IsVisible = true;

            _blockTimer = new Timer(1000);
            _blockTimer.Elapsed += (s, e) =>
            {
                remainingSeconds--;

                Dispatcher.UIThread.Post(() =>
                {
                    if (remainingSeconds > 0)
                    {
                        TimerText.Text = $"Блокировка: {remainingSeconds} сек";
                    }
                    else
                    {
                        // Останавливаем таймер
                        _blockTimer.Stop();
                        _blockTimer.Dispose();

                        // Разблокируем
                        _isBlocked = false;
                        MainPanel.IsEnabled = true;
                        TimerText.IsVisible = false;
                        TimerText.Text = "";
                    }
                });
            };
            _blockTimer.Start();
        }

        private void ButtonGuest(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Application.Current.Resources["CurrentUser"] = null;
            var windowMenu = new WindowMenu();
            windowMenu.Show();
            Close();
        }

       private void GenerateCaptcha()
        {
            CaphaCanvas.Children.Clear();

            var random = new Random();
            var chars = "1234567890";
            _caphaText = "";

            for (int i = 0; i < 4; i++)
            {
                _caphaText += chars[random.Next(chars.Length)];
            }

            for (int i = 0; i < 6; i++)
            {
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = new Point(random.Next(0, 50), random.Next(10, 40)),
                    EndPoint = new Point(random.Next(200, 250), random.Next(10, 40)),

                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2,

                    Opacity = 0.5,
                };
                CaphaCanvas.Children.Add(line);
            }

            for (int i = 0; i < 20; i++)
            {
                var ellipse = new Avalonia.Controls.Shapes.Ellipse
                {
                    Width = 2,
                    Height = 2,
                    Fill = Brushes.Gray
                };
                Canvas.SetLeft(ellipse, random.Next(0, 270));
                Canvas.SetTop(ellipse, random.Next(10, 30));
                CaphaCanvas.Children.Add(ellipse);
            }
            double xPos = 50;

            for (int i = 0; i < _caphaText.Length; i++)
            {
                var textBlock = new TextBlock
                {
                    Text = _caphaText[i].ToString(),
                    //FontSize = random.Next(10, 25),
                    //FontWeight = random.Next(2) == 0 ? FontWeight.Normal : FontWeight.Bold,
                    Foreground = Brushes.Black,
                    //RenderTransform = new TransformGroup
                    //{
                    //    Children = new Transforms
                    //    {
                    //        new RotateTransform(random.Next(-15, 15)),
                    //        new ScaleTransform(random.Next(8, 16) / 10.0   , 1.0)
                    //    }
                    //}
                };
                Canvas.SetLeft(textBlock, xPos + random.Next(-3, 3));
                Canvas.SetTop(textBlock, 10 + random.Next(-3, 8));

                CaphaCanvas.Children.Add(textBlock);
                xPos += 40 + random.Next(-5, 10);
            }
        }


        private void ButtonUpdateCapha(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (!_isBlocked)
            {
                GenerateCaptcha();
                CaphaText.Text = "";
            }
        }
    }
}
