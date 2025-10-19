using EnergyTask;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using EnergyTask.Models;
using System.Data.SQLite;

namespace EnergyTask.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddEmpPage.xaml
    /// </summary>
    public partial class AddEmpPage : Page
    {
        private Employees newEmployee;
        private List<Departments> departments;
        private DateTime selectedBirthDate;
        private DateTime selectedStartDate;

        public AddEmpPage()
        {
            InitializeComponent();

            // Создание нового сотрудника
            newEmployee = new Employees();

            // Устанавливаем даты по умолчанию
            selectedBirthDate = DateTime.Today.AddYears(-18);
            selectedStartDate = DateTime.Today;

            // Устанавливаем даты в DatePicker'ы
            BirthDatePicker.SelectedDate = selectedBirthDate;
            StartDatePicker.SelectedDate = selectedStartDate;

            this.DataContext = newEmployee;
            LoadDepartments();
        }

        private void LoadDepartments()
        {
            try
            {
                departments = new List<Departments>();
                string sql = "SELECT * FROM Departments";

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            departments.Add(new Departments
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                ParentId = reader.IsDBNull(reader.GetOrdinal("ParentId")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("ParentId"))
                            });
                        }
                    }
                }

                DepartmentComboBox.ItemsSource = departments;
                DepartmentComboBox.DisplayMemberPath = "Name";
                DepartmentComboBox.SelectedValuePath = "Id";

                if (departments.Any())
                    DepartmentComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отделов: {ex.Message}");
            }
        }

        private void AddPhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";
            openFileDialog.Title = "Выберите фотографию сотрудника";

            if (openFileDialog.ShowDialog() == true)
            {
                string sourcePath = openFileDialog.FileName;

                try
                {
                    string safeFileName = CreateSafeFileName(sourcePath);

                    // Сохранение фото в папку Photo рядом с исполняемым файлом
                    string debugPath = Directory.GetCurrentDirectory();
                    string photoFolder = Path.Combine(debugPath, "Photo");
                    string photoPath = Path.Combine(photoFolder, safeFileName);

                    // Создаем папку если не существует
                    if (!Directory.Exists(photoFolder))
                    {
                        Directory.CreateDirectory(photoFolder);
                    }

                    // Копируем фото
                    File.Copy(sourcePath, photoPath, true);

                    // Сохраняем только имя файла в БД
                    newEmployee.Photo = safeFileName;

                    // Показываем фото в интерфейсе
                    EmployeePhoto.Source = new BitmapImage(new Uri(photoPath));

                    MessageBox.Show("Фото загружено успешно");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке фото: {ex.Message}");
                }
            }
        }

        private string CreateSafeFileName(string sourcePath)
        {
            string extension = Path.GetExtension(sourcePath);
            string safeName = $"photo_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}";
            return safeName;
        }

        private void CancelAddEmpBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SaveAddEmpBtn_Click(object sender, RoutedEventArgs e)
        {
            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(newEmployee.FullName))
            {
                MessageBox.Show("Введите ФИО сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(newEmployee.Position))
            {
                MessageBox.Show("Введите должность сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (BirthDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату начала работы", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DepartmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите подразделение", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime today = DateTime.Today;
            DateTime minBirthDate = today.AddYears(-18);

            // Проверка возраста
            if (BirthDatePicker.SelectedDate.Value > minBirthDate)
            {
                MessageBox.Show("Сотруднику должно быть не менее 18 лет", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка даты начала работы
            if (StartDatePicker.SelectedDate.Value > today)
            {
                MessageBox.Show("Дата начала работы не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Устанавливаем DepartmentId из ComboBox'а
                if (DepartmentComboBox.SelectedItem is Departments selectedDepartment)
                {
                    newEmployee.DepartmentId = selectedDepartment.Id;
                }

                // Сохраняем сотрудника в базу данных
                string sql = @"INSERT INTO Employees 
                             (DepartmentId, FullName, Position, BirthDate, Photo, StartDate) 
                             VALUES (@DepartmentId, @FullName, @Position, @BirthDate, @Photo, @StartDate);
                             SELECT last_insert_rowid();";

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DepartmentId", newEmployee.DepartmentId);
                        command.Parameters.AddWithValue("@FullName", newEmployee.FullName);
                        command.Parameters.AddWithValue("@Position", newEmployee.Position);
                        command.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@Photo", newEmployee.Photo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@StartDate", StartDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd"));

                        // Получаем ID нового сотрудника
                        newEmployee.Id = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                MessageBox.Show("Сотрудник успешно добавлен", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении сотрудника: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики изменений DatePicker'ов
        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BirthDatePicker.SelectedDate.HasValue)
            {
                selectedBirthDate = BirthDatePicker.SelectedDate.Value;
            }
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue)
            {
                selectedStartDate = StartDatePicker.SelectedDate.Value;
            }
        }

        private void DepartmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentComboBox.SelectedItem is Departments selectedDepartment)
            {
                newEmployee.DepartmentId = selectedDepartment.Id;
            }
        }
    }
}
