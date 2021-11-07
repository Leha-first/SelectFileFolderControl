using System.Collections.Generic;
using ASCRV.CommonClasses;
using System.Collections.ObjectModel;


namespace ASCRV.CommonControls.SelectFileFolderControl
{
    public class Model
    {
        /// <summary>
        /// Основной список с директориями и файлами
        /// </summary>
        public ObservableCollection<CurrentDirectoryWithFiles> ListWithDirectiveFiles { get; set; }

        /// <summary>
        /// Список со стандартными расширениями для обеспечения фильтра
        /// </summary>
        public List<string> ListWithDefaultFilterExtension { get; set; }
        /// <summary>
        /// Логический флаг - нужно ли показывать файлы
        /// </summary>
        public bool IsShowFiles { get; set; }
        /// <summary>
        /// Наименование файл, требуемого для зарузки
        /// </summary>
        public string FilterExtension { get; set; }
    }
}
