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
    /// Логика взаимодействия для EditEmpPage.xaml
    /// </summary>
    public partial class EditEmpPage : Page
    {
        private Employees selectedEmployee;
        private List<Departments> departments;

        public EditEmpPage(Employees employee)
        {
            InitializeComponent();

            selectedEmployee = employee;
            this.DataContext = selectedEmployee;

            LoadData();
            LoadPhoto();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем отделы из базы данных
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

                // Устанавливаем выбранный отдел
                DepartmentComboBox.SelectedValue = selectedEmployee.DepartmentId;

                // Устанавливаем даты в DatePicker'ы
                BirthDatePicker.SelectedDate = selectedEmployee.BirthDate;
                StartDatePicker.SelectedDate = selectedEmployee.StartDate;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadPhoto()
        {
            if (!string.IsNullOrEmpty(selectedEmployee.Photo))
            {
                string photoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EnergyTask", "Photo", selectedEmployee.Photo);

                if (File.Exists(photoPath))
                {
                    try
                    {
                        EmployeePhoto.Source = new BitmapImage(new Uri(photoPath));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки фото: {ex.Message}");
                    }
                }
            }
        }

        private void ChangePhotoBtn_Click(object sender, RoutedEventArgs e)
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

                    // Путь к папке Photo в AppData
                    string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string appFolder = Path.Combine(appDataPath, "EnergyTask", "Photo");
                    string projectPhotoPath = Path.Combine(appFolder, safeFileName);

                    if (!Directory.Exists(appFolder))
                    {
                        Directory.CreateDirectory(appFolder);
                    }

                    File.Copy(sourcePath, projectPhotoPath, true);

                    selectedEmployee.Photo = safeFileName;
                    EmployeePhoto.Source = new BitmapImage(new Uri(projectPhotoPath));

                    MessageBox.Show("Фото обновлено успешно");
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

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(selectedEmployee.FullName) ||
                string.IsNullOrWhiteSpace(selectedEmployee.Position) ||
                BirthDatePicker.SelectedDate == null ||
                StartDatePicker.SelectedDate == null ||
                DepartmentComboBox.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля");
                return;
            }

            DateTime today = DateTime.Today;
            DateTime minBirthDate = today.AddYears(-18);

            if (BirthDatePicker.SelectedDate.Value > minBirthDate)
            {
                MessageBox.Show("Сотруднику должно быть не менее 18 лет");
                return;
            }

            if (StartDatePicker.SelectedDate.Value > today)
            {
                MessageBox.Show("Дата начала работы не может быть в будущем");
                return;
            }

            try
            {
                // Обновляем даты из DatePicker'ов
                selectedEmployee.BirthDate = BirthDatePicker.SelectedDate.Value;
                selectedEmployee.StartDate = StartDatePicker.SelectedDate.Value;

                // Обновляем DepartmentId из ComboBox'а
                if (DepartmentComboBox.SelectedItem is Departments selectedDepartment)
                {
                    selectedEmployee.DepartmentId = selectedDepartment.Id;
                }

                // Сохраняем изменения в базе данных
                string sql = @"UPDATE Employees 
                             SET FullName = @FullName, 
                                 Position = @Position, 
                                 BirthDate = @BirthDate, 
                                 StartDate = @StartDate, 
                                 Photo = @Photo, 
                                 DepartmentId = @DepartmentId 
                             WHERE Id = @Id";

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", selectedEmployee.FullName);
                        command.Parameters.AddWithValue("@Position", selectedEmployee.Position);
                        command.Parameters.AddWithValue("@BirthDate", selectedEmployee.BirthDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@StartDate", selectedEmployee.StartDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@Photo", selectedEmployee.Photo ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DepartmentId", selectedEmployee.DepartmentId);
                        command.Parameters.AddWithValue("@Id", selectedEmployee.Id);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Данные сохранены");
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Удалить сотрудника {selectedEmployee.FullName}?",
                "Подтверждение",
                MessageBoxButton.YesNo
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string sql = "DELETE FROM Employees WHERE Id = @Id";

                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        using (var command = new SQLiteCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Id", selectedEmployee.Id);
                            command.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Сотрудник удален");
                    NavigationService.GoBack();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}");
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        // Обработчики изменений DatePicker'ов
        private void BirthDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BirthDatePicker.SelectedDate.HasValue)
            {
                selectedEmployee.BirthDate = BirthDatePicker.SelectedDate.Value;
            }
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StartDatePicker.SelectedDate.HasValue)
            {
                selectedEmployee.StartDate = StartDatePicker.SelectedDate.Value;
            }
        }

        private void DepartmentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepartmentComboBox.SelectedItem is Departments selectedDepartment)
            {
                selectedEmployee.DepartmentId = selectedDepartment.Id;
            }
        }
    }
}
