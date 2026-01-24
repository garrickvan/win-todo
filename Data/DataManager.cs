using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinTodo.Data
{
    /// <summary>
    /// 任务项类，包含任务的所有属性
    /// </summary>
    public class TaskItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Group { get; set; }
        public string? Description { get; set; }
        public bool Completed { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Priority { get; set; }
    }

    /// <summary>
    /// JSON序列化上下文，用于编译时生成序列化代码，避免反射禁用问题
    /// </summary>
    [JsonSerializable(typeof(List<TaskItem>))]
    [JsonSerializable(typeof(TaskItem))]
    internal partial class TaskItemContext : JsonSerializerContext
    { }

    /// <summary>
    /// 数据管理器，负责处理待办事项的数据持久化和管理
    /// </summary>
    public class DataManager
    {
        private const string DataFileName = "tasks.json";
        private readonly string _dataFilePath;
        private List<TaskItem> _tasks = new List<TaskItem>();

        /// <summary>
        /// 初始化数据管理器
        /// </summary>
        public DataManager()
        {
            try
            {
                // 使用PathHelper确保数据目录存在并获取数据文件路径
                PathHelper.EnsureLocalAppDataDirectoryExists();
                _dataFilePath = PathHelper.GetDataFilePath(DataFileName);
                LoadData();
            }
            catch (Exception ex)
            {
                LogHelper.LogError(ex, "初始化数据目录失败");
                // 如果无法创建目录，仍然继续执行，确保应用能够启动
                _dataFilePath = PathHelper.GetDataFilePath(DataFileName);
                _tasks = new List<TaskItem>();
            }
        }

        /// <summary>
        /// 从文件加载数据
        /// </summary>
        private void LoadData()
        {
            if (File.Exists(_dataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_dataFilePath);
                    _tasks = JsonSerializer.Deserialize(json, TaskItemContext.Default.ListTaskItem) ?? new List<TaskItem>();
                }
                catch (Exception ex)
                {
                    LogHelper.LogError(ex, "反序列化数据失败");
                    _tasks = new List<TaskItem>();
                }
            }
            else
            {
                _tasks = new List<TaskItem>();
            }
        }

        /// <summary>
        /// 保存数据到文件
        /// </summary>
        private void SaveData()
        {
            try
            {
                // 使用编译时生成的JsonSerializerContext，避免反射禁用问题
                var options = new JsonSerializerOptions
                {
                    TypeInfoResolver = TaskItemContext.Default,
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_tasks, options);
                File.WriteAllText(_dataFilePath, json);
            }
            catch { /* 忽略保存错误 */ }
        }

        /// <summary>
        /// 添加新任务
        /// </summary>
        /// <param name="title">任务标题</param>
        /// <param name="group">任务分组</param>
        /// <param name="description">任务描述</param>
        /// <returns>添加的任务项</returns>
        public TaskItem AddTask(string title, string group, string description)
        {
            int newId = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1;

            var task = new TaskItem
            {
                Id = newId,
                Title = title,
                Group = group,
                Description = description,
                Completed = false,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Priority = 0
            };

            _tasks.Add(task);
            SaveData();
            return task;
        }

        /// <summary>
        /// 更新任务标题
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="newTitle">新标题</param>
        public void UpdateTaskTitle(int taskId, string newTitle)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Title = newTitle;
                task.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        /// <summary>
        /// 更新任务状态
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="completed">是否完成</param>
        public void UpdateTaskStatus(int taskId, bool completed)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Completed = completed;
                task.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        /// <summary>
        /// 删除任务到回收站
        /// </summary>
        /// <param name="taskId">任务ID</param>
        public void DeleteTask(int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsDeleted = true;
                task.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        /// <summary>
        /// 从回收站恢复任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        public void RestoreTask(int taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.IsDeleted = false;
                task.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        /// <summary>
        /// 永久删除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        public void PermanentDeleteTask(int taskId)
        {
            _tasks.RemoveAll(t => t.Id == taskId);
            SaveData();
        }

        /// <summary>
        /// 更新任务优先级
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="priority">优先级值（0=一般，1=重要，2=紧急）</param>
        public void UpdateTaskPriority(int taskId, int priority)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                task.Priority = priority;
                task.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        /// <returns>任务列表</returns>
        public List<TaskItem> GetAllTasks()
        {
            return _tasks.Where(t => !t.IsDeleted).ToList();
        }

        /// <summary>
        /// 鑾峰彇浠婃棩浠诲姟
        /// </summary>
        /// <returns>浠婃棩浠诲姟鍒楄〃</returns>
        public List<TaskItem> GetTodayTasks()
        {
            DateTime today = DateTime.Today;
            return _tasks.Where(t => !t.IsDeleted && t.CreatedAt.Date == today).ToList();
        }

        /// <summary>
        /// 按分组获取任务
        /// </summary>
        /// <param name="group">分组名称</param>
        /// <returns>任务列表</returns>
        public List<TaskItem> GetTasksByGroup(string group)
        {
            return _tasks.Where(t => t.Group == group && !t.IsDeleted).ToList();
        }

        /// <summary>
        /// 获取回收站任务
        /// </summary>
        /// <returns>回收站任务列表</returns>
        public List<TaskItem> GetRecycleBinTasks()
        {
            return _tasks.Where(t => t.IsDeleted).ToList();
        }

        /// <summary>
        /// 获取任务统计
        /// </summary>
        /// <returns>任务统计字典</returns>
        public Dictionary<string, int> GetTaskCount()
        {
            int total = _tasks.Where(t => !t.IsDeleted).Count();
            int completed = _tasks.Where(t => t.Completed && !t.IsDeleted).Count();
            int pending = total - completed;

            return new Dictionary<string, int>
            {
                { "total", total },
                { "completed", completed },
                { "pending", pending }
            };
        }

        /// <summary>
        /// 获取指定分类的未完成任务数量
        /// </summary>
        /// <param name="group">分类名称</param>
        /// <returns>未完成任务数量</returns>
        public int GetPendingTasksByGroup(string group)
        {
            return _tasks.Where(t => t.Group == group && !t.IsDeleted && !t.Completed).Count();
        }
    }
}

