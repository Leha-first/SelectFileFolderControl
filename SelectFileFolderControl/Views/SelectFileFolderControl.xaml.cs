using System;
using System.Linq;
using System.Windows;
using CommonExtensions;
using ASCRV.CommonClasses;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Eventing.Reader;
using System.Threading;

namespace ASCRV.CommonControls.SelectFileFolderControl
{
    /// <summary>
    /// Логика взаимодействия для SelectFileFolderControl.xaml
    /// </summary>
    public partial class SelectFileFolderControl : UserControl
    {
        private Model _model;
        private bool AllTasksByDirectoryCompleted;
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public SelectFileFolderControl()
        {
            InitializeComponent();
            FilesAndFolders.RaiseEventCheckDelegateHandler += FilesAndFolders_RaiseEventCheckDelegateHandler;
            FilesAndFolders.RaiseAllNetworkTasksIsCompletedDelegateHandler += FilesAndFolders_RaiseAllNetworkTasksIsCompletedDelegateHandler;
            FilesAndFolders.RaiseTasksLoadChildsElementsCompletedDelegateHandler += FilesAndFolders_RaiseTasksLoadChildsElementsCompletedDelegateHandler;
            ComboBoxWithFilesExtensions.SelectionChanged += ComboBoxWithFilesExtensionsSelectionChanged;
        }

        private void FilesAndFolders_RaiseTasksLoadChildsElementsCompletedDelegateHandler(List<System.Threading.Tasks.Task> listWithTasks)
        {
            AllTasksByDirectoryCompleted = true;
        }

        /// <summary>
        /// Событие возникает при завершении работы всех потоков при загрузке директорий
        /// </summary>
        private void FilesAndFolders_RaiseAllNetworkTasksIsCompletedDelegateHandler()
        {
            var networkRoot = _model.ListWithDirectiveFiles.FirstOrDefault(s => s.DirectoryName == "Сеть(загрузка элементов...)");
            if (networkRoot == null) return;
            networkRoot.DirectoryName = "Сеть";
        }

        /// <summary>
        /// Событие выбора в поле 
        /// </summary>
        /// <param name="sender"> Combobox "ComboBoxWithFilesExtensions" </param>
        /// <param name="e"> Данные о событии SelectionChangedEventArgs </param>
        private void ComboBoxWithFilesExtensionsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _model.FilterExtension = ((ComboBox) sender).SelectedItem.ToString();

            foreach (var elem in _model.ListWithDirectiveFiles)
                FilterFilesBySelectedExtension(elem);
        }

        /// <summary>
        /// Фильтр элементов
        /// </summary>
        /// <param name="rootDir"> Исходная директория </param>
        private void FilterFilesBySelectedExtension(CurrentDirectoryWithFiles rootDir)
        {
            var newColl = new List<CurrentFile>();
            var completeExt = "";
            var mask = new List<string>();

            if (_model.FilterExtension != "Все файлы")
            {
                var splitInputValue = _model.FilterExtension.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                if (splitInputValue == null) return;
                var maskWithExtensionArray = splitInputValue.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if(!maskWithExtensionArray.Any()) return;
                var ext = maskWithExtensionArray.LastOrDefault();
                if (ext == null) return;
                completeExt = "." + ext;
                maskWithExtensionArray.Remove(ext);
                var splitedMask = maskWithExtensionArray.FirstOrDefault()?.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (splitedMask != null)
                    if (splitedMask.Count >= 1)
                        splitedMask.ForEach(s => mask.Add(s));
            }

            if (!mask.Any())
            {
                if (_model.FilterExtension == "Все файлы" || string.IsNullOrEmpty(completeExt))
                    newColl = rootDir.DirectoryFiles;
                else
                    newColl = rootDir.DirectoryFiles.Where(s => s.FileExtension == completeExt).ToList();
            }
            else
                foreach (var mas in mask)
                    newColl = rootDir.DirectoryFiles.Where(s => s.FileExtension == completeExt && s.FileName.Contains(mas)).ToList();
            //ПОИСК ОТМЕЧЕННЫХ ФАЙЛОВ ДЛЯ СНЯТИЯ СЕЛЕКТА ПРИ ФИЛЬТРЕ
            var checkedFiles = rootDir.DirectoryAndFilesElements.Where(s => s is CurrentFile)
                .Where(s => ((CurrentFile) s).IsChecked)
                .ToList();
            //ЕСЛИ НОВЫЙ ФИЛЬТР СОДЕРЖИТ ОТМЕЧЕННЫЙ ФАЙЛ, ОСТАВЛЯЕМ ЕГО ОТМЕЧЕННЫМ
            foreach (var checkFile in checkedFiles)
                if (!newColl.Contains(checkFile)) ((CurrentFile) checkFile).IsChecked = false;
            
            rootDir.DirectoryAndFilesElements.RemoveAll(s => s is CurrentFile);

            foreach (var file in newColl)
                rootDir.DirectoryAndFilesElements.Add(file);

            foreach (var subDir in rootDir.DirectorySubDirectories)
                FilterFilesBySelectedExtension(subDir);
        }

        /// <summary>
        /// Фильтр элементов при пустом выборе фильтра - загрузка файлов с расширением из СПИСКА
        /// </summary>
        /// <param name="rootDir"> Исходная директория </param>
        private void FilterFilesBySelectedExtensionByList(CurrentDirectoryWithFiles rootDir)
        {
            var newColl = new List<CurrentFile>();

            foreach (var filterExtension in _model.ListWithDefaultFilterExtension)
            {
                var completeExt = "";
                var mask = new List<string>();

                if (filterExtension != "Все файлы")
                {
                    var splitInputValue = filterExtension.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    if (splitInputValue == null) return;
                    var maskWithExtensionArray = splitInputValue.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (!maskWithExtensionArray.Any()) return;
                    var ext = maskWithExtensionArray.LastOrDefault();
                    if (ext == null) return;
                    completeExt = "." + ext;
                    maskWithExtensionArray.Remove(ext);
                    var splitedMask = maskWithExtensionArray.FirstOrDefault()?.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (splitedMask != null)
                        if (splitedMask.Count >= 1)
                            splitedMask.ForEach(s => mask.Add(s));
                }

                if (!mask.Any())
                {
                    if (filterExtension == "Все файлы" || string.IsNullOrEmpty(completeExt))
                    { newColl.AddRange(rootDir.DirectoryFiles); break; }
                    else
                        newColl.AddRange(rootDir.DirectoryFiles.Where(s => s.FileExtension == completeExt).ToList());
                }
                else
                    foreach (var mas in mask)
                        newColl.AddRange(rootDir.DirectoryFiles.Where(s => s.FileExtension == completeExt && s.FileName.Contains(mas)).ToList());
            }

            rootDir.DirectoryAndFilesElements.RemoveAll(s => s is CurrentFile);

            foreach (var file in newColl)
                rootDir.DirectoryAndFilesElements.Add(file);

            foreach (var subDir in rootDir.DirectorySubDirectories)
                FilterFilesBySelectedExtensionByList(subDir);
        }


        /// <summary>
        /// Заполнение коллекции с расширениями
        /// </summary>
        private void FillCollectionWithExtensions() =>
            ComboBoxWithFilesExtensions.ItemsSource = _model.ListWithDefaultFilterExtension;

        /// <summary>
        /// Статическое событие при чеке элемента
        /// </summary>
        /// <param name="item"> Элемент с которого НЕ ТРЕБУЕТСЯ снимать чек </param>
        private void FilesAndFolders_RaiseEventCheckDelegateHandler(object item)
        {
            if (item == null) { Application.Current.Dispatcher?.Invoke(new Action(() => { TextBoxWithSelectedPath.Text = string.Empty; })); return; }
            TextBoxWithSelectedPath.Text = string.Empty;
            if (item is CurrentDirectoryWithFiles itIsDirectoryWithFiles)
                foreach (var dir in _model.ListWithDirectiveFiles)
                {
                    if (dir != itIsDirectoryWithFiles) dir.IsChecked = false;
                    foreach (var directory in dir.DirectorySubDirectories)
                        UnchekedDirectories(directory, itIsDirectoryWithFiles);
                }
            else
                foreach (var dir in _model.ListWithDirectiveFiles)
                {
                    foreach (var file in dir.DirectoryFiles.Where(s => s != (CurrentFile)item))
                        file.IsChecked = false;
                    foreach (var directory in dir.DirectorySubDirectories)
                        UnchekedFiles(directory, (CurrentFile)item);
                }
            //ВЫВОД В ТЕКСТОВОЕ ПОЛЕ ПУТИ К ВЫБРАННОМУ ЭЛЕМЕНТУ
            Application.Current.Dispatcher?.Invoke(new Action(() => { TextBoxWithSelectedPath.Text = item is CurrentDirectoryWithFiles cdf ? cdf.DirectoryPath : ((CurrentFile) item).FilePath; }));
        }

        /// <summary>
        /// Рекурсивный метод снятия чека с директории и его дочерних элементов
        /// </summary>
        /// <param name="directory"> Директория </param>
        /// <param name="item"> Элемент, с которого не требуется снимать чек </param>
        private void UnchekedDirectories(CurrentDirectoryWithFiles directory, CurrentDirectoryWithFiles item)
        {
            if (directory != item) directory.IsChecked = false;
            foreach (var dir in directory.DirectorySubDirectories)
                UnchekedDirectories(dir, item);
        }

        /// <summary>
        /// Рекурсивный метод снятия чека с файла и его дочерних элементов
        /// </summary>
        /// <param name="directory"> Директория </param>
        /// <param name="item"> Элемент, с которого не требуется снимать чек </param>
        private void UnchekedFiles(CurrentDirectoryWithFiles directory, CurrentFile item)
        {
            foreach (var file in directory.DirectoryFiles.Where(s => s != item))
                file.IsChecked = false;

            foreach (var dir in directory.DirectorySubDirectories)
                UnchekedFiles(dir, item);
        }

        /// <summary>
        /// Получение выбранного пользователем элемента
        /// </summary>
        /// <returns> Список с выброанными элементами </returns>
        public List<string> GetSelectedItem()
        {
            if (_model.IsShowFiles)
            {
                CurrentFile curFile = null;

                foreach (var item in _model.ListWithDirectiveFiles.Where(s=>s.IsChildAlreadyLoaded))
                {
                    SearchCheckedFile(item, ref curFile);
                    if (curFile != null) break;
                }

                return curFile != null ? new List<string> { curFile.FilePath } : new List<string>();
            }

            CurrentDirectoryWithFiles selEl = null;
            foreach (var item in _model.ListWithDirectiveFiles.Where(s => s.IsChildAlreadyLoaded || s.IsChecked))
            {
                if (item.IsChecked) { selEl = item; break; }
                SearchCheckedDirectory(item, ref selEl);
                if (selEl != null) break;
            }

            return selEl != null ? new List<string> { selEl.DirectoryPath } : new List<string>();
        }

        /// <summary>
        /// Метод рекурсивного поиска отмеченной директории
        /// </summary>
        /// <param name="rootItem"> Входной элемент </param>
        /// <param name="searchedElement"> Найденный элемент </param>
        private void SearchCheckedDirectory(CurrentDirectoryWithFiles rootItem, ref CurrentDirectoryWithFiles searchedElement)
        {
            foreach (var subItem in rootItem.DirectorySubDirectories.Where(s => s.IsChildAlreadyLoaded || s.IsChecked))
            {
                if (subItem.IsChecked) searchedElement = subItem;
                if (searchedElement == null)
                    SearchCheckedDirectory(subItem, ref searchedElement);
            }
        }

        /// <summary>
        /// Метод рекурсивного поиска отмеченного файла
        /// </summary>
        /// <param name="rootItem"> Директория поиска </param>
        /// <param name="curFile"></param>
        /// <returns> Отмеченный файл </returns>
        private void SearchCheckedFile(CurrentDirectoryWithFiles rootItem, ref CurrentFile curFile)
        {
            var searchedFile = rootItem.DirectoryFiles.FirstOrDefault(s => s.IsChecked);
            if (searchedFile != null) curFile = searchedFile;

            foreach (var subDir in rootItem.DirectorySubDirectories.Where(s => s.IsChildAlreadyLoaded))
            {
                foreach (var file in subDir.DirectoryFiles)
                    if (file.IsChecked) curFile = file;

                if (curFile == null)
                    SearchCheckedFile(subDir, ref curFile);
            }
        }

        /// <summary>
        /// Установка данных контрола
        /// </summary>
        /// <param name="isShowFiles"> Требуется ли показывать файлы </param>
        /// <param name="isShowNetwork"> Требуется ли показывать элементы в сети </param>
        /// <param name="requiredExtensions"> Требуемые расширения </param>
        /// <param name="selectedExtension"> Выбираемое по умолчанию расширение </param>
        /// <param name="isGiveUserToChooseFilterFileExtension"> Давать ли пользователю возможность выбора из списка выбора с расширениями </param>
        public void SetDataControl(bool isShowFiles, bool isShowNetwork, List<string> requiredExtensions = null, string selectedExtension = null,
            bool isGiveUserToChooseFilterFileExtension = false)
        {
            _model = new Model
            {
                ListWithDirectiveFiles = new ObservableCollection<CurrentDirectoryWithFiles>(),
                ListWithDefaultFilterExtension = new List<string>
                    {"Все файлы", ".jpg", ".png", ".bmp", ".exe", ".docx", ".docx", ".xlsx", ".exe", ".pdf", ".txt"},
                IsShowFiles = isShowFiles
            };
            DataContext = _model;
            //ЕСЛИ ФАЙЛЫ НЕ НАДО ПОКАЗЫВАТЬ, СКРЫТЬ ГРИД С ВЫБОРОМ РАСШИРЕНИЯ ФАЙЛА
            if (isShowFiles)
            {
                //ЕСЛИ ПЕРЕДАН СПИСОК С ТРЕБУЕМЫМИ РАСШИРЕНИЯМИ ФАЙЛОВ ДЛЯ ФИЛЬТРОВ
                if (requiredExtensions != null)
                {
                    _model.ListWithDefaultFilterExtension.Clear();
                    _model.ListWithDefaultFilterExtension.AddRange(requiredExtensions);
                }

                //ПРОВЕРКА ПАРАМЕТРА - СЛЕДУЕТ ЛИ ПОЛЬЗОВАТЕЛЮ ДАВАТЬ ВОЗМОЖНОСТЬ ВЫБОРА РАСШИРЕНИЯ ДЛЯ ФИЛЬТРА
                if (isGiveUserToChooseFilterFileExtension)
                {
                    FillCollectionWithExtensions();
                    if (selectedExtension != null)
                        ComboBoxWithFilesExtensions.SelectedItem = selectedExtension;
                    GridWithFieldExtension.Visibility = Visibility.Visible;
                }
                else
                {
                    if (_model.ListWithDefaultFilterExtension.Contains("Все файлы")) { TextBlockWithSelectedFiles.Text = "Все файлы"; }
                    else _model.ListWithDefaultFilterExtension.ForEach(s => TextBlockWithSelectedFiles.Text += s + "; ");
                    GridWithTextBlockFilterExtension.Visibility = Visibility.Visible;
                }
            }

            var logDrives = FilesAndFolders.GetLogicalDrivesList();
            foreach (var drive in logDrives)
            {
                var driveAdding = new CurrentDirectoryWithFiles
                {
                    DirectoryPath = drive.DriveLetter,
                    DirectoryName = drive.DriveLetter,
                    DirectoryType = DirectoryTypes.Ordinary,
                    DirectoryFiles = new List<CurrentFile>(),
                    DirectorySubDirectories = new List<CurrentDirectoryWithFiles>(),
                    DirectoryAndFilesElements = new ObservableCollection<object>(),
                    IsVisible = _model.IsShowFiles ? Visibility.Collapsed : Visibility.Visible
                };
                driveAdding.RaiseChekedItemEvent += FilesAndFolders_RaiseEventCheckDelegateHandler;
                _model.ListWithDirectiveFiles.Add(driveAdding);
                FilesAndFolders.GetChildFilesAndDirectories(driveAdding, true, _model.IsShowFiles);
            }

            if (isShowNetwork)
            {
                var networkAdding = new CurrentDirectoryWithFiles
                {
                    DirectoryPath = "",
                    DirectoryName = "Сеть(загрузка элементов...)",
                    DirectoryFiles = new List<CurrentFile>(),
                    DirectorySubDirectories = new List<CurrentDirectoryWithFiles>(),
                    DirectoryAndFilesElements = new ObservableCollection<object>(),
                    IsVisible = Visibility.Collapsed,
                    IsChildAlreadyLoaded = true
                };
                networkAdding.RaiseChekedItemEvent += FilesAndFolders_RaiseEventCheckDelegateHandler;

                _model.ListWithDirectiveFiles.Add(networkAdding);
                FilesAndFolders.ListNetworkComputers(networkAdding, _model.IsShowFiles);
            }
        }

        /// <summary>
        /// Событие происходит при раскрытии элемента в дереве
        /// </summary>
        /// <param name="sender"> TreeView "TreeViewSelectFolderFilesElements" </param>
        /// <param name="e"> Данные о событии RoutedEventArgs </param>
        private void TreeViewSelectFolderFilesElements_OnExpanded(object sender, RoutedEventArgs e)
        {
           //ПОЛУЧЕНИЕ ТЕКУЩЕГО ЭКЗЕМПЛЯРА ДИРЕКТОРИИ С ФАЙЛАМИ
           var it = (CurrentDirectoryWithFiles)((TreeViewItem)e.OriginalSource).Header;
            if (it == null) return;
            if (it.IsChildAlreadyLoaded == false) it.IsChildAlreadyLoaded = true;
            else return;
            //ДОБАВЛЕНИЕ В ДОЧЕРНИЕ ПАПКИ ФАЙЛОВ И ПАПОК
            foreach (var subDirectories in it.DirectorySubDirectories)
                FilesAndFolders.GetChildFilesAndDirectories(subDirectories, false, _model.IsShowFiles);

            if (_model.IsShowFiles)
            {
                if (_model.FilterExtension == null) FilterFilesBySelectedExtensionByList(it);
                else
                    FilterFilesBySelectedExtension(it);
            }
        }

        /// <summary>
        /// Событие возникает при нажатии на кнопку "Перейти"
        /// </summary>
        /// <param name="sender"> Button "ButtonGoToPath" </param>
        /// <param name="e"> Данные о событии RoutedEventArgs </param>
        private void ButtonGoToPath_OnClick(object sender, RoutedEventArgs e)
        {
            TextBlockWithWarning.Text = string.Empty;
            var inputPath = TextBoxWithSelectedPath.Text;
            if (!inputPath.Any())
            {
                TextBlockWithWarning.Text = "Введен пустой строковый запрос...";
                return;
            }
            var arSplitFolders = inputPath.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (arSplitFolders.Count == 0) return;

            var root = _model.ListWithDirectiveFiles.FirstOrDefault(s => s.DirectoryPath.Replace("\\", "") == arSplitFolders.FirstOrDefault());
            if (root == null) return;
            //if (root.DirectoryName == "Сеть(загрузка элементов...)")
            //{
            //    TextBlockWithWarning.Text = "Директории требуемого компьютера домена еще не загружены...";
            //    return;
            //}
            //ОЖИДАНИЕ, ЧТО ЗАКОНЧАТСЯ ТАСКИ(ЕСЛИ ПОДДИРЕКТОРИЙ НЕТ - ТАСКИ ЗАПУЩЕНЫ НЕ БУДУТ - ПРИСВОЕНИЕ АВТОМАТИЧЕСКИ)
            AllTasksByDirectoryCompleted = !root.DirectorySubDirectories.Any() || root.IsChildAlreadyLoaded;
            root.IsExpanded = true;
            //на случай жесткого диска, например, C:\
            if (arSplitFolders.Count == 1) { root.IsChecked = true; return; }

            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                while (!AllTasksByDirectoryCompleted) { }

                var lastPath = arSplitFolders.LastOrDefault();
                if (lastPath == null) return;
                if (lastPath.Contains("."))
                {
                    CurrentFile curFile = null;
                    SearchFileByPath(root, inputPath, ref curFile);
                    if (curFile != null)
                    {
                        curFile.IsChecked = true;
                        Application.Current.Dispatcher?.Invoke(new Action(() => { TreeViewSelectFolderFilesElements.BringIntoView(); }));
                    }
                    else
                        Application.Current.Dispatcher?.Invoke(new Action(() => {
                            TextBlockWithWarning.Text = "Указанный файл не обнаружен. Проверьте корректность имени файла или пути...";}));
                }
                else
                {
                    CurrentDirectoryWithFiles curDir = null;
                    SearchDirectoryByPath(root, inputPath, ref curDir);
                    if (curDir != null)
                    {
                        if (_model.IsShowFiles) curDir.IsExpanded = true;
                        else curDir.IsChecked = true;
                        Application.Current.Dispatcher?.Invoke(new Action(() => { TreeViewSelectFolderFilesElements.BringIntoView(); }));
                    }
                    else
                        Application.Current.Dispatcher?.Invoke(new Action(() => {
                            TextBlockWithWarning.Text = "Указанная директория не обнаружена. Проверьте корректность имени директории или пути...";}));
                }
            });
        }

        /// <summary>
        /// Метод рекурсивного поиска элемента
        /// </summary>
        /// <param name="rootItem"> Директория поиска </param>
        /// <param name="path"> Путь к элементу </param>
        /// <param name="curFile"> Текущий файл </param>
        /// <returns> Элемент по пути </returns>
        private void SearchFileByPath(CurrentDirectoryWithFiles rootItem, string path, ref CurrentFile curFile)
        {
            var searchedFile = rootItem.DirectoryFiles.FirstOrDefault(s =>
                s.FilePath.Split(new[] {'/', '\\'}).SequenceEqual(path.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries)));
            if (searchedFile != null) { curFile = searchedFile; return; }

            //ДИРЕКТОРИЯ, КОТОРАЯ ДОЛЖНА БЫТЬ РАСКРЫТА
            var dirForExpand = rootItem.DirectorySubDirectories.FirstOrDefault(s => path.Contains(s.DirectoryPath));
            if (dirForExpand == null) return;
            //ОЖИДАНИЕ, ЧТО ЗАКОНЧАТСЯ ТАСКИ(ЕСЛИ ПОДДИРЕКТОРИЙ НЕТ - ТАСКИ ЗАПУЩЕНЫ НЕ БУДУТ - ПРИСВОЕНИЕ АВТОМАТИЧЕСКИ)
            AllTasksByDirectoryCompleted = !dirForExpand.DirectorySubDirectories.Any() || dirForExpand.IsChildAlreadyLoaded;
            dirForExpand.IsExpanded = true;
            while (!AllTasksByDirectoryCompleted) { }

            var directoriesWithChilds = rootItem.DirectorySubDirectories.Where(s => s.IsChildAlreadyLoaded).ToList();

            foreach (var subDir in directoriesWithChilds)
            {
                //foreach (var file in subDir.DirectoryFiles)
                //    if (file.FilePath == path) curFile = file;

                if (curFile == null)
                    SearchFileByPath(subDir, path, ref curFile);
            }
        }

        /// <summary>
        /// Метод рекурсивного поиска элемента
        /// </summary>
        /// <param name="rootItem"> Директория поиска </param>
        /// <param name="path"> Путь к элементу </param>
        /// <param name="curDir"> Текущая директория </param>
        /// <returns> Элемент по пути </returns>
        private void SearchDirectoryByPath(CurrentDirectoryWithFiles rootItem, string path,  ref CurrentDirectoryWithFiles curDir)
        {
            var searchedFile = rootItem.DirectorySubDirectories.FirstOrDefault(s =>
                s.DirectoryPath.Split(new[] {'/', '\\'}).SequenceEqual(path.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)));
            if (searchedFile != null) { curDir = searchedFile; return; }

            //ДИРЕКТОРИЯ, КОТОРАЯ ДОЛЖНА БЫТЬ РАСКРЫТА
            var dirForExpand = rootItem.DirectorySubDirectories.FirstOrDefault(s => path.Contains(s.DirectoryPath));
            if (dirForExpand == null) return;
            //ОЖИДАНИЕ, ЧТО ЗАКОНЧАТСЯ ТАСКИ(ЕСЛИ ПОДДИРЕКТОРИЙ НЕТ - ТАСКИ ЗАПУЩЕНЫ НЕ БУДУТ - ПРИСВОЕНИЕ АВТОМАТИЧЕСКИ)
            AllTasksByDirectoryCompleted = !dirForExpand.DirectorySubDirectories.Any() || dirForExpand.IsChildAlreadyLoaded;
            dirForExpand.IsExpanded = true;
            while (!AllTasksByDirectoryCompleted) { }//ОЖИДАНИЕ ВЫПОЛНЕНИЯ ТАСКОВ

            var direcroriesWithChilds = rootItem.DirectorySubDirectories.Where(s => s.IsChildAlreadyLoaded).ToList();

            foreach (var subDir in direcroriesWithChilds)
            {
                //foreach (var dir in subDir.DirectorySubDirectories)
                //    if (dir.DirectoryPath == path) curDir = dir;

                if (curDir == null)
                    SearchDirectoryByPath(subDir, path, ref curDir);
            }
        }
    }
}
