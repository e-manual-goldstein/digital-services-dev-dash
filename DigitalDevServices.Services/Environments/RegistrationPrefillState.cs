using DigitalDevServices.Model.Environments;

namespace DigitalDevServices.Services.Environments;

public sealed class RegistrationPrefillState : IRegistrationPrefillState
{
    public RemoteRegistrationPrefill? Current { get; private set; }

    public void Set(RemoteRegistrationPrefill prefill) => Current = prefill;

    public RemoteRegistrationPrefill? Take()
    {
        var prefill = Current;
        Current = null;
        return prefill;
    }

    public void Clear() => Current = null;
}
