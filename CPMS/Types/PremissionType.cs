namespace CPMS.Types
{
    public enum PremissionType
    {
        // 大多數CURD，都只有管理層有權限做。 

        // For Table Manage
        CreateTable,
        DeleteTable,
        ModifyTable,
        ModifyStatusTable,
        QueryTable,
        QueryTableByDuty, // 值班人員有權限查看該台的資料

        // For User Manage
        CreateUser,
        DeleteUser,
        ModifyUser,
        QueryUser,
        QueryUserOwner, // 員工有權限查自己的資料

        // For Game Manage
        CreateGame,
        DeleteGame,
        ModifyGame,
        QueryGame,
        QueryGameByDuty, // 值班人員有權限查看該遊戲的資料
    }
}
