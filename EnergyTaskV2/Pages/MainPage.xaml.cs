using EnergyTask;
using EnergyTask.Models;
using EnergyTask.Windows;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EnergyTask.Pages
{
    /// <summary>
    /// Логика взаимодействия для MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private List<Departments> departments = new List<Departments>();
        private List<Employees> employees = new List<Employees>();
        private TreeViewItem _selectedTreeItem;

        public MainPage()
        {
            InitializeComponent();
            LoadDepartments();
            LoadEmployees();
        }

        private void LoadDepartments()
        {
            try
            {
                departments.Clear();
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

                BuildDepartmentTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки подразделений: {ex.Message}");
            }
        }

        private void LoadEmployees()
        {
            try
            {
                employees.Clear();
                string sql = "SELECT * FROM Employees";

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();
                    using (var command = new SQLiteCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new Employees
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                                Position = reader.GetString(reader.GetOrdinal("Position")),
                                BirthDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("BirthDate"))),
                                Photo = reader.IsDBNull(reader.GetOrdinal("Photo")) ? null : reader.GetString(reader.GetOrdinal("Photo")),
                                StartDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("StartDate")))
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private void BuildDepartmentTree()
        {
            DepartmentsTreeView.Items.Clear();

            // Нахождение корневых подразделений
            var rootDepartments = departments.Where(d => d.ParentId == null).ToList();

            foreach (var department in rootDepartments)
            {
                var treeViewItem = CreateTreeViewItem(department);
                DepartmentsTreeView.Items.Add(treeViewItem);
            }
        }

        private TreeViewItem CreateTreeViewItem(Departments department)
        {
            var item = new TreeViewItem
            {
                Header = $"{department.Name}",
                Tag = department,
                IsExpanded = true,
                FontSize = 12
            };

            // дочерние подразделения
            var childDepartments = departments.Where(d => d.ParentId == department.Id).ToList();
            foreach (var child in childDepartments)
            {
                var childItem = CreateTreeViewItem(child);
                item.Items.Add(childItem);
            }

            return item;
        }

        private void DepartmentsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeViewItem selectedItem && selectedItem.Tag is Departments selectedDepartment)
            {
                ShowEmployeesInDepartment(selectedDepartment);
            }
        }

        private void ShowEmployeesInDepartment(Departments department)
        {
            EmployeesHeader.Text = $"Сотрудники: {department.Name}";

            var employeesInDepartment = employees.Where(emp => emp.DepartmentId == department.Id).ToList();
            EmployeesLV.ItemsSource = employeesInDepartment;
            UpdateStatistics(employeesInDepartment);
        }

        private void UpdateStatistics(List<Employees> departmentEmployees)
        {
            if (departmentEmployees.Any())
            {
                string FormatDate(double totalDays)
                {
                    int years = (int)(totalDays / 365.25);
                    int months = (int)((totalDays % 365.25) / 30.44);

                    if (years == 0 && months == 0) return "менее 1 месяца";
                    if (years == 0) return $"{months} мес.";
                    if (months == 0) return $"{years} лет";
                    return $"{years} лет {months} мес.";
                }

                var averageAgeDays = departmentEmployees.Average(emp =>
                    (DateTime.Now - emp.BirthDate).TotalDays);
                var averageExperienceDays = departmentEmployees.Average(emp =>
                    (DateTime.Now - emp.StartDate).TotalDays);

                EmployeeCountText.Text = $"{departmentEmployees.Count}";
                AverageAgeText.Text = $"{FormatDate(averageAgeDays)}";
                AverageExperienceText.Text = $"{FormatDate(averageExperienceDays)}";
            }
            else
            {
                EmployeeCountText.Text = "0";
                AverageAgeText.Text = "0";
                AverageExperienceText.Text = "0";
            }
        }

        // Обработка правой кнопки мыши для контекстного меню
        private void DepartmentsTreeView_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = GetTreeViewItemUnderMouse(e.GetPosition(DepartmentsTreeView));
            if (treeViewItem != null)
            {
                treeViewItem.IsSelected = true;
                _selectedTreeItem = treeViewItem;

                ShowContextMenu(treeViewItem);
            }
        }

        private TreeViewItem GetTreeViewItemUnderMouse(Point point)
        {
            var hitTest = VisualTreeHelper.HitTest(DepartmentsTreeView, point);
            if (hitTest != null)
            {
                var treeViewItem = FindParent<TreeViewItem>(hitTest.VisualHit);
                return treeViewItem;
            }
            return null;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null && !(child is T))
                child = VisualTreeHelper.GetParent(child);
            return child as T;
        }

        private void ShowContextMenu(TreeViewItem treeViewItem)
        {
            var contextMenu = new ContextMenu();

            var addMenu = new MenuItem { Header = "➕ Добавить подразделение" };
            addMenu.Click += AddDepartmentMenu_Click;
            contextMenu.Items.Add(addMenu);

            var editMenu = new MenuItem { Header = "✏️ Редактировать" };
            editMenu.Click += EditDepartmentMenu_Click;
            contextMenu.Items.Add(editMenu);

            var deleteMenu = new MenuItem { Header = "❌ Удалить" };
            deleteMenu.Click += DeleteDepartmentMenu_Click;
            contextMenu.Items.Add(deleteMenu);

            treeViewItem.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        // Обработчики контекстного меню
        private void AddDepartmentMenu_Click(object sender, RoutedEventArgs e)
        {
            var parentDepartment = _selectedTreeItem?.Tag as Departments;
            ShowDepartmentDialog(null, parentDepartment?.Id);
        }

        private void EditDepartmentMenu_Click(object sender, RoutedEventArgs e)
        {
            var selectedDepartment = _selectedTreeItem?.Tag as Departments;
            if (selectedDepartment != null)
            {
                ShowDepartmentDialog(selectedDepartment);
            }
        }

        private void DeleteDepartmentMenu_Click(object sender, RoutedEventArgs e)
        {
            var selectedDepartment = _selectedTreeItem?.Tag as Departments;
            if (selectedDepartment != null)
            {
                DeleteDepartment(selectedDepartment);
            }
        }

        private void ShowDepartmentDialog(Departments department = null, int? parentId = null)
        {
            try
            {
                var dialog = new ManageDepartmentWindow(department, departments, parentId);

                if (dialog.ShowDialog() == true)
                {
                    SaveDepartment(dialog.Department);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveDepartment(Departments department)
        {
            try
            {
                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    if (department.Id == 0) // Новое подразделение
                    {
                        string sql = @"INSERT INTO Departments (Name, ParentId) 
                                     VALUES (@Name, @ParentId);
                                     SELECT last_insert_rowid();";

                        using (var command = new SQLiteCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Name", department.Name);
                            command.Parameters.AddWithValue("@ParentId", department.ParentId ?? (object)DBNull.Value);

                            department.Id = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }
                    else
                    {
                        string sql = @"UPDATE Departments 
                                     SET Name = @Name, ParentId = @ParentId 
                                     WHERE Id = @Id";

                        using (var command = new SQLiteCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Name", department.Name);
                            command.Parameters.AddWithValue("@ParentId", department.ParentId ?? (object)DBNull.Value);
                            command.Parameters.AddWithValue("@Id", department.Id);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                LoadDepartments();

                MessageBox.Show("Подразделение сохранено", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDepartment(Departments department)
        {
            try
            {
                // Проверяем, есть ли дочерние подразделения
                bool hasChildDepartments = departments.Any(d => d.ParentId == department.Id);
                if (hasChildDepartments)
                {
                    MessageBox.Show("Нельзя удалить подразделение: есть дочерние подразделения",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Проверяем, есть ли сотрудники
                bool hasEmployees = employees.Any(emp => emp.DepartmentId == department.Id);
                if (hasEmployees)
                {
                    MessageBox.Show("Нельзя удалить подразделение: есть сотрудники",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    $"Удалить подразделение '{department.Name}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    using (var connection = DatabaseHelper.GetConnection())
                    {
                        connection.Open();
                        string sql = "DELETE FROM Departments WHERE Id = @Id";

                        using (var command = new SQLiteCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("@Id", department.Id);
                            command.ExecuteNonQuery();
                        }
                    }

                    LoadDepartments();
                    MessageBox.Show("Подразделение удалено", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddEmpBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddEmpPage());
        }

        private void AddDepartmentBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowDepartmentDialog();
        }

        private void RefreshEmpBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadDepartments();
            LoadEmployees();
            MessageBox.Show("Данные обновлены", "Обновление", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EmployeesLV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedEmployee = EmployeesLV.SelectedItem as Employees;
            if (selectedEmployee != null)
            {
                NavigationService.Navigate(new EditEmpPage(selectedEmployee));
            }
        }

        private void EditEmpBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedEmployee = button?.DataContext as Employees;
            if (selectedEmployee != null)
            {
                NavigationService.Navigate(new EditEmpPage(selectedEmployee));
            }
            else
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}


