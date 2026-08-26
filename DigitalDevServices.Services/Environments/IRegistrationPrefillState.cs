using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public interface IRegistrationPrefillState
{
    RemoteRegistrationPrefill? Current { get; }

    void Set(RemoteRegistrationPrefill prefill);

    RemoteRegistrationPrefill? Take();

    void Clear();
}
