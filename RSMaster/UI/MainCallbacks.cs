namespace RSMaster.UI
{
    public partial class MainWindow
    {
        internal static LogCallback LogHandler;
        internal static ShutdownCallback ShutdownHandler;
        internal static UpdateAccountCallback UpdateAccountHandler;
        internal static GetSelectedAccountCallback GetSelectedAccountHandler;
        internal static LaunchAccountCallback LaunchAccountHandler;
        internal static GetGroupByIdCallback GetGroupByIdHandler;
        internal static GetAccountsCallback GetAccountsHandler;
        internal static AccountCreatedHandler AccountCreatedCallback;
        internal static StopAccount StopAccountCallback;
    }
}
