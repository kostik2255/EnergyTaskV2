using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace EnergyTask.Models
{
    public class Employees
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string FullName { get; set; }
        public string Position { get; set; }
        public DateTime BirthDate { get; set; }
        public string Photo { get; set; }
        public DateTime StartDate { get; set; }

        public BitmapImage PhotoImage
        {
            get
            {
                if (!string.IsNullOrEmpty(Photo))
                {
                    try
                    {
                        // Путь к фото в папке AppData
                        string photoPath = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "EnergyTask", "Photo", Photo);

                        if (System.IO.File.Exists(photoPath))
                        {
                            return new BitmapImage(new Uri(photoPath));
                        }
                    }
                    catch
                    {

                    }
                }
                return null;
            }
        }
    }
}
