using CompareHub.Backend.app.Core.Domain.Entities;
using CompareHub.Backend.app.Core.Domain.Specifications;

namespace CompareHub.Backend.app.Core.Modules.Auth.Specifications;

public sealed class AppUserByGoogleSubjectSpecification : BaseSpecification<AppUser>
{
    public AppUserByGoogleSubjectSpecification(string subject)
    {
        AddCriteria(x => x.GoogleSubject == subject);
    }
}
