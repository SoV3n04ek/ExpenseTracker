using ExpenseTracker.Application.Interfaces;

namespace ExpenseTracker.Application.Services
{
    public abstract class BaseService
    {
        private readonly ICurrentUserService _currentUserService;

        protected BaseService(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        protected int CurrentUserId
        {
            get
            {
                var userId = _currentUserService.UserId;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var result))
                {
                    // This should ideally be caught by the ValidateUserFilter, 
                    // but we provide a fallback/safety check here.
                    return 0; 
                }
                return result;
            }
        }
    }
}
