namespace FP.ContainerTraining.EventOperator.Authentication;

public class UserAccountService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserAccountService> _logger;

    public UserAccountService(IConfiguration configuration, ILogger<UserAccountService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool TryCreateUserSession(string user, string password, out UserSession? userSession)
    {
        userSession = null;
        if (string.IsNullOrEmpty(user) || 
            string.IsNullOrEmpty(password))
        {
            _logger.LogInformation("username or password is empty");
            return false;
        }
        var userAccounts = _configuration.GetSection("Users").Get<UserAccountConfiguration[]>();
        if (userAccounts == null)
        {
            _logger.LogWarning("Unable to load users from configuration");
            return false;
        }

        var userAccount = userAccounts.FirstOrDefault(ua =>
            string.Equals(ua.User, user, StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(ua.Password, password, StringComparison.InvariantCulture));

        if (userAccount == null)
        {
            _logger.LogWarning("unable to find user {Username} with the given password", user);
            return false;
        }
        userSession = new UserSession
        {
            Name = userAccount.User,
            Role = userAccount.Role
        };
        return true;
    }
}