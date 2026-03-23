using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Demo09.Context;
using Demo09.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Demo09;

public partial class WindowItemProduct : Window
{
    private WindowMenu _windowMenu;
    private string _resourcesPath;
    private string _fileName;
    private string _selectedImagePath; // Путь к выбранному изображению

    public WindowItemProduct()
    {
        InitializeComponent();

        var context = new PostgresContext();
        ComboboxCategory.ItemsSource = context.Categories.Select(c => c.Name).ToList();
        ComboboxManufacture.ItemsSource = context.Manufacturers.Select(c => c.Name).ToList();
        ComboboxUnit.ItemsSource = context.Products.Select(c => c.Unit).Distinct().ToList();

        var user = Application.Current.Resources["CurrentUser"] as User;

        if (user == null)
        {
            TextNameUser.Text = "Гость";

        }
        else
        {
            TextNameUser.Text = user.Name;

        }
    }

    public WindowItemProduct(Product product, WindowMenu windowMenu)
    {
        InitializeComponent();
        _windowMenu = windowMenu;
        DataContext = product;
        ButSaveName.IsVisible = false;

        // Инициализируем путь к ресурсам
        _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource");
        Directory.CreateDirectory(_resourcesPath);

        // Загружаем фото
        LoadProductImage(product.Articul);

        var context = new PostgresContext();
        ComboboxCategory.ItemsSource = context.Categories.Select(c => c.Name).ToList();
        ComboboxManufacture.ItemsSource = context.Manufacturers.Select(c => c.Name).ToList();
        ComboboxUnit.ItemsSource = context.Products.Select(c => c.Unit).Distinct().ToList();

        var user = Application.Current.Resources["CurrentUser"] as User;

        if (user == null)
        {
            TextNameUser.Text = "Гость";

        }
        else
        {
            TextNameUser.Text = user.Name;

        }
    }

    public WindowItemProduct(WindowMenu windowMenu)
    {
        InitializeComponent();
        _windowMenu = windowMenu;
        DataContext = new Product();
        ButUpdName.IsVisible = false;

        // Инициализируем путь к ресурсам
        _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resource");
        Directory.CreateDirectory(_resourcesPath);

        var uri = new Uri($"avares://Demo09/Resource/picture.png");
        var asset = AssetLoader.Open(uri);
        ProductImage.Source = new Bitmap(asset);

        var context = new PostgresContext();
        ComboboxCategory.ItemsSource = context.Categories.Select(c => c.Name).ToList();
        ComboboxManufacture.ItemsSource = context.Manufacturers.Select(c => c.Name).ToList();
        ComboboxUnit.ItemsSource = context.Products.Select(c => c.Unit).Distinct().ToList();

        var user = Application.Current.Resources["CurrentUser"] as User;

        if (user == null)
        {
            TextNameUser.Text = "Гость";

        }
        else
        {
            TextNameUser.Text = user.Name;

        }
    }

    private void ButSave(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var product = DataContext as Product;

        var context = new PostgresContext();



        product.Manufacturer = context.Manufacturers.FirstOrDefault(x => x.Name == ComboboxManufacture.SelectedItem!.ToString())!;
        product.Unit = context.Products.FirstOrDefault(x => x.Unit == ComboboxUnit.SelectedItem).Unit;
        product.Category = context.Categories.FirstOrDefault(x => x.Name == ComboboxCategory.SelectedItem!.ToString())!;

        context.Products.Add(product);
        context.SaveChanges();

        // Сохраняем изображение после сохранения продукта, чтобы был артикул
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            SaveImageToResource(product.Articul, _selectedImagePath);
        }

        _windowMenu.GetInfo();
        Close();
    }

    private void ButUpdate(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var product = DataContext as Product;

        var context = new PostgresContext();

        // Присоединяем продукт к контексту
        context.Products.Attach(product);

        product.Manufacturer = context.Manufacturers.FirstOrDefault(x => x.Name == ComboboxManufacture.SelectedItem!.ToString())!;
        product.Unit = context.Products.FirstOrDefault(x => x.Unit == ComboboxUnit.SelectedItem).Unit;
        product.Category = context.Categories.FirstOrDefault(x => x.Name == ComboboxCategory.SelectedItem!.ToString())!;

        // Убираем дублирование: оставляем только Update или только Entry.State
        context.Products.Update(product);

        context.SaveChanges();

        // Если выбрано новое изображение, сохраняем его
        if (!string.IsNullOrEmpty(_selectedImagePath))
        {
            SaveImageToResource(product.Articul, _selectedImagePath);
        }

        _windowMenu.GetInfo();
        Close();
    }

    private async void Butremove(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var product = DataContext as Product;

        var context = new PostgresContext();
        var productOrder = context.OrderProducts.Where(op => op.Product.Id == product.Id).FirstOrDefault();

        if (productOrder != null)
        {
            await MessageBoxManager.GetMessageBoxStandard("Уведомление", "Продукт заказан, нельзя удалить!", ButtonEnum.Ok).ShowWindowDialogAsync(this);
            return;
        }

        var res = await MessageBoxManager.GetMessageBoxStandard("Уведомление", "Вы хотите удалить элемент?", ButtonEnum.YesNo).ShowWindowDialogAsync(this);

        if (res == ButtonResult.Yes)
        {
            //var context = new PostgresContext();
            context.Products.Remove(product);
            context.SaveChanges();

            // Удаляем изображение продукта
            DeleteProductImage(product.Articul);

            _windowMenu.GetInfo();
            Close();
        }
    }





    // Загрузка сохраненного фото
    private void LoadProductImage(string articul)
    {
        var product = DataContext as Product;
        if (product != null)
        {
            ProductImage.Source = product.ProductImage; // Используем свойство из модели
        }
        else
        {
            // Заглушка
            var uri = new Uri($"avares://Demo09/Resource/picture.png");
            var asset = AssetLoader.Open(uri);
            ProductImage.Source = new Bitmap(asset);
        }
    }

    private async void ButtonLoadImage(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Открываем диалог выбора файлов
            var dialog = new OpenFileDialog();
            dialog.Title = "Выберите изображение";
            dialog.Filters.Add(new FileDialogFilter() { Name = "Изображения", Extensions = { "jpg", "jpeg", "png", "bmp", "gif" } });
            dialog.AllowMultiple = false;

            // Получаем главное окно для показа диалога
            var mainWindow = this;

            string[]? result = await dialog.ShowAsync(mainWindow);

            if (result != null && result.Length > 0)
            {
                string selectedFile = result[0];

                // Проверяем, что файл существует
                if (File.Exists(selectedFile))
                {
                    _selectedImagePath = selectedFile;

                    // Показываем выбранное изображение в интерфейсе
                    using (var stream = File.OpenRead(selectedFile))
                    {
                        ProductImage.Source = new Bitmap(stream);
                    }

                    // Показываем сообщение об успешной загрузке
                    await MessageBoxManager.GetMessageBoxStandard("Успешно", "Изображение выбрано. После сохранения оно будет добавлено.", ButtonEnum.Ok).ShowWindowDialogAsync(this);
                }
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Не удалось загрузить изображение: {ex.Message}", ButtonEnum.Ok).ShowWindowDialogAsync(this);
        }
    }

    // Сохранение изображения в папку Resource
    private void SaveImageToResource(string articul, string sourceImagePath)
    {
        try
        {
            string extension = Path.GetExtension(sourceImagePath).ToLower();
            string destinationPath = Path.Combine(_resourcesPath, $"{articul}{extension}");

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Copy(sourceImagePath, destinationPath);

            LoadProductImage(articul);
        }
        catch (Exception ex)
        {
            MessageBoxManager.GetMessageBoxStandard("Ошибка", $"Не удалось сохранить изображение: {ex.Message}", ButtonEnum.Ok).ShowWindowDialogAsync(this);
        }
    }

    // Удаление изображения продукта
    private void DeleteProductImage(string articul)
    {
        try
        {
            string imagePath = Path.Combine(_resourcesPath, $"{articul}.jpg");
            if (File.Exists(imagePath))
            {
                File.Delete(imagePath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при удалении изображения: {ex.Message}");
        }
    }





    private void ButBack(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

}




