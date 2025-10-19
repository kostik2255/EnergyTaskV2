using EnergyTask;
using EnergyTask.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace EnergyTask.Windows
{
    /// <summary>
    /// Логика взаимодействия для ManageDepartmentWindow.xaml
    /// </summary>
    public partial class ManageDepartmentWindow : Window, INotifyPropertyChanged
    {
        public Departments Department { get; set; }
        public List<Departments> AllDepartments { get; set; }
        private string _currentLevel;

        public ManageDepartmentWindow(Departments department = null, List<Departments> allDepartments = null, int? parentId = null)
        {
            InitializeComponent();

            Department = department ?? new Departments();
            AllDepartments = allDepartments ?? new List<Departments>();

            // Определяем уровень нового подразделения
            if (department == null && parentId.HasValue)
            {
                var parent = AllDepartments.FirstOrDefault(d => d.Id == parentId.Value);
                _currentLevel = GetNextLevel(parent);
            }
            else if (department != null)
            {
                _currentLevel = DetermineCurrentLevel(department.Name);
            }
            else
            {
                _currentLevel = "Служба"; // Корневой уровень
            }

            this.DataContext = this;
            LoadParentDepartments();
            ApplyLevelPrefix();
        }

        private string GetNextLevel(Departments parent)
        {
            if (parent == null) return "Служба";

            var parentLevel = DetermineCurrentLevel(parent.Name);

            switch (parentLevel)
            {
                case "Служба": return "Управление";
                case "Управление": return "Отдел";
                case "Отдел": return "Группа";
                case "Группа": return "Группа"; // Группа не может иметь дочерних
                default: return "Служба";
            }
        }

        private string DetermineCurrentLevel(string departmentName)
        {
            if (string.IsNullOrEmpty(departmentName)) return "Служба";
            if (departmentName.StartsWith("Служба")) return "Служба";
            else if (departmentName.StartsWith("Управление")) return "Управление";
            else if (departmentName.StartsWith("Отдел")) return "Отдел";
            else if (departmentName.StartsWith("Группа")) return "Группа";
            else return "Служба";
        }

        private void ApplyLevelPrefix()
        {
            if (Department.Id == 0) // Новое подразделение
            {
                DepartmentName = _currentLevel + " ";
            }
            else if (!string.IsNullOrEmpty(DepartmentName) && !DepartmentName.StartsWith(_currentLevel))
            {
                // Убираем старый префикс и добавляем новый
                var nameWithoutPrefix = RemoveLevelPrefix(DepartmentName);
                DepartmentName = _currentLevel + " " + nameWithoutPrefix;
            }
        }

        private string RemoveLevelPrefix(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (name.StartsWith("Служба ")) return name.Substring(7);
            if (name.StartsWith("Управление ")) return name.Substring(11);
            if (name.StartsWith("Отдел ")) return name.Substring(6);
            if (name.StartsWith("Группа ")) return name.Substring(7);
            return name;
        }

        private void LoadParentDepartments()
        {
            // Для службы родитель не нужен
            if (_currentLevel == "Служба")
            {
                ParentDepCB.IsEnabled = false;
                ParentInfoText.Text = "Служба - корневой уровень (не требует родителя)";
                ParentDepCB.ItemsSource = null;
                return;
            }

            // Определяем требуемый тип родительского подразделения
            string requiredParentType = GetRequiredParentType(_currentLevel);
            ParentInfoText.Text = $"Требуется {requiredParentType} как родительское подразделение";

            // Фильтруем подразделения по требуемому типу
            var availableParents = AllDepartments
                .Where(d => !string.IsNullOrEmpty(d.Name) && d.Name.StartsWith(requiredParentType))
                .ToList();

            // Исключаем текущее подразделение и его потомков
            if (Department.Id > 0)
            {
                availableParents = availableParents
                    .Where(d => d.Id != Department.Id && !IsChildDepartment(Department.Id, d.Id))
                    .ToList();
            }

            ParentDepCB.ItemsSource = availableParents;
            ParentDepCB.DisplayMemberPath = "Name";
            ParentDepCB.SelectedValuePath = "Id";
            ParentDepCB.IsEnabled = true;

            // Устанавливаем родителя из конструктора или текущего значения
            if (Department.ParentId.HasValue)
            {
                ParentDepCB.SelectedValue = Department.ParentId.Value;
            }
            else if (availableParents.Count == 1)
            {
                ParentDepCB.SelectedIndex = 0;
            }
        }

        private string GetRequiredParentType(string level)
        {
            switch (level)
            {
                case "Управление": return "Служба";
                case "Отдел": return "Управление";
                case "Группа": return "Отдел";
                default: return "";
            }
        }

        private bool IsChildDepartment(int parentId, int checkDepartmentId)
        {
            var children = AllDepartments.Where(d => d.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                if (child.Id == checkDepartmentId || IsChildDepartment(child.Id, checkDepartmentId))
                    return true;
            }
            return false;
        }

        public string DialogTitle => Department.Id == 0 ? "Добавление подразделения" : "Редактирование подразделения";

        private string _departmentName;
        public string DepartmentName
        {
            get => _departmentName ?? Department?.Name ?? "";
            set
            {
                _departmentName = value;
                if (Department != null)
                    Department.Name = value;
                OnPropertyChanged(nameof(DepartmentName));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        private int? _parentId;
        public int? ParentId
        {
            get
            {
                if (_parentId.HasValue) return _parentId;
                return Department?.ParentId;
            }
            set
            {
                _parentId = value;
                if (Department != null)
                    Department.ParentId = value;
                OnPropertyChanged(nameof(ParentId));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool CanSave => !string.IsNullOrWhiteSpace(DepartmentName?.Trim()) &&
                              DepartmentName.Trim().Length > _currentLevel.Length + 1;

        private void SaveDepBtn_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что название начинается с правильного префикса
            if (!string.IsNullOrEmpty(DepartmentName) && !DepartmentName.StartsWith(_currentLevel))
            {
                var nameWithoutPrefix = RemoveLevelPrefix(DepartmentName);
                DepartmentName = _currentLevel + " " + nameWithoutPrefix;
            }

            // Проверяем, что есть название после префикса
            if (string.IsNullOrWhiteSpace(DepartmentName) || DepartmentName.Trim().Length <= _currentLevel.Length + 1)
            {
                MessageBox.Show("Введите название подразделения после префикса", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем наличие родителя для не-служб
            if (_currentLevel != "Служба" && ParentId == null)
            {
                string requiredParent = GetRequiredParentType(_currentLevel);
                MessageBox.Show($"Для {_currentLevel} необходимо выбрать {requiredParent} как родительское подразделение",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем, что группа не имеет дочерних подразделений (при редактировании)
            if (_currentLevel == "Группа" && Department.Id > 0)
            {
                var hasChildren = AllDepartments.Any(d => d.ParentId == Department.Id);
                if (hasChildren)
                {
                    MessageBox.Show("Группа не может иметь дочерних подразделений", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ParentDepCB_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ParentDepCB.SelectedItem is Departments selectedDepartment)
            {
                ParentId = selectedDepartment.Id;
            }
            else
            {
                ParentId = null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

