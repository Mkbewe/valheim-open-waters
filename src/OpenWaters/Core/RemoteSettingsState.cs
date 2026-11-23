using System;

namespace OpenWaters
{
    internal enum RemoteSettingsStatus
    {
        NotReceived,
        NoSnapshot,
        SeedMismatch,
        InvalidPayload,
        IncompatibleGenerator,
        Accepted
    }

    internal sealed class RemoteSettingsState
    {
        private WorldSettings _settings;
        private string _seed;
        private bool _received;
        private bool _invalid;

        public void Clear()
        {
            _settings = null;
            _seed = null;
            _received = false;
            _invalid = false;
        }

        public void MarkPeerInfoReceived()
        {
            _settings = null;
            _seed = null;
            _invalid = false;
            _received = true;
        }

        public void MarkPayloadInvalid()
        {
            _settings = null;
            _seed = null;
            _invalid = true;
            _received = true;
        }

        public void Set(WorldSettings settings, string seed)
        {
            _settings = settings;
            _seed = seed;
            _invalid = false;
            _received = true;
        }

        public RemoteSettingsStatus Resolve(string seed, out WorldSettings settings)
        {
            settings = _settings;
            if (!_received)
            {
                return RemoteSettingsStatus.NotReceived;
            }
            if (_invalid)
            {
                return RemoteSettingsStatus.InvalidPayload;
            }
            if (_settings == null)
            {
                return RemoteSettingsStatus.NoSnapshot;
            }
            if (!string.Equals(_seed, seed, StringComparison.Ordinal))
            {
                return RemoteSettingsStatus.SeedMismatch;
            }
            if (!_settings.IsGeneratorCompatible)
            {
                return RemoteSettingsStatus.IncompatibleGenerator;
            }
            return RemoteSettingsStatus.Accepted;
        }
    }
}
