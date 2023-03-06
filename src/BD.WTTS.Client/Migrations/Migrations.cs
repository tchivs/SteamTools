// ReSharper disable once CheckNamespace
namespace BD.WTTS;

/// <summary>
/// 新版本破坏性更改迁移
/// </summary>
public static class Migrations
{
    public const string DirName_Scripts = "Scripts";

    static Version? PreviousVersion { get; } = Version.TryParse(VersionTracking2.PreviousVersion, out var value) ? value : null;

    /// <summary>
    /// 开始迁移(在 DI 初始化完毕后调用)
    /// </summary>
    public static void Up()
    {
        // 可以删除 /AppData/application2.dbf 表 0984415E 使此 if 返回 True
        // 目前此数据库中仅有这一张表，所以可以直接删除文件，但之后可能会新增新表
        if (VersionTracking2.IsFirstLaunchForCurrentVersion) // 当前版本号第一次启动
        {
            if (PreviousVersion != null) // 上一次运行的版本小于 2.6.2 时将执行以下迁移
            {
                if (PreviousVersion < new Version(2, 6, 2))
                {
                    try
                    {
                        var cacheScript = Path.Combine(IOPath.CacheDirectory, DirName_Scripts);
                        if (Directory.Exists(cacheScript))
                            Directory.Delete(cacheScript, true);
                    }
                    catch (Exception e)
                    {
                        Log.Error(nameof(Migrations), e, "RemoveJSCache");
                    }
                }

                if (OperatingSystem.IsWindows() &&
                    !DesktopBridge.IsRunningAsUwp &&
                    PreviousVersion < new Version(2, 7, 3)) // 上一次运行的版本小于 2.7.3 时将执行以下迁移
                {

                    try
                    {
                        var binPath = Path.Combine(AppContext.BaseDirectory, "Bin");
                        if (Directory.Exists(binPath))
                            Directory.Delete(binPath, true);
                    }
                    catch (Exception e)
                    {
                        Log.Error(nameof(Migrations), e, "RemovebinPath");
                    }
                }
            }
        }
    }
}