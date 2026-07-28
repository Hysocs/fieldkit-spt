
namespace FieldKit
{
    public sealed partial class Plugin
    {
        private void UpdateForcedVisionModes()
        {
            if (_forceThermalVision == null ||
                _forceNightVision == null)
            {
                return;
            }

            bool requested =
                _forceThermalVision.Value ||
                _forceNightVision.Value ||
                (_forceThermalVision.Value &&
                 _cleanThermalVision.Value) ||
                (_forceNightVision.Value &&
                 (_cleanNightVision.Value ||
                  !Mathf.Approximately(
                      _nightVisionBloomAmount.Value, 1f))) ||
                _forcedVisorMode.Value !=
                    ForcedVisorMode.FollowEquipment;
            if (!requested)
            {
                if (_visionOverridesNeedUpdate)
                    RestoreForcedVisionOverrides();
                return;
            }
            if (_forceThermalVision.Value &&
                _forceNightVision.Value)
            {
                _forceNightVision.Value = false;
            }

            CameraClass camera = CameraClass.Instance;
            if (camera == null)
                return;

            try
            {
                ThermalVision thermal = camera.ThermalVision;
                CaptureThermalVisionSettings(thermal);
                if (thermal != null &&
                    thermal.On != _forceThermalVision.Value)
                {
                    thermal.On = _forceThermalVision.Value;
                }
                ApplyThermalImageSettings(thermal);

                BSG.CameraEffects.NightVision nightVision =
                    camera.NightVision;
                CaptureNightVisionSettings(nightVision);
                CaptureNightVisionBloom(camera);
                if (nightVision != null &&
                    nightVision.On != _forceNightVision.Value)
                {
                    nightVision.On = _forceNightVision.Value;
                }
                ApplyNightVisionImageSettings(nightVision);
                ApplyNightVisionBloom();
                UpdateForcedVisorOverlay(camera);
                _visionOverridesNeedUpdate = requested;
            }
            catch (Exception exception)
            {
                LogSource.LogWarning(
                    "Could not update forced camera vision: " +
                    exception.Message);
            }
        }

        private void CaptureThermalVisionSettings(
            ThermalVision thermal)
        {
            if (thermal == null ||
                ReferenceEquals(thermal, _configuredThermalVision))
            {
                return;
            }

            RestoreThermalImageSettings();
            _configuredThermalVision = thermal;
            _originalThermalOn = thermal.On;
            _originalThermalNoise = thermal.IsNoisy;
            _originalThermalFpsStuck = thermal.IsFpsStuck;
            _originalThermalMotionBlur = thermal.IsMotionBlurred;
            _originalThermalGlitch = thermal.IsGlitch;
            _originalThermalPixelation = thermal.IsPixelated;
        }

        private void ApplyThermalImageSettings(
            ThermalVision thermal)
        {
            if (thermal == null)
                return;

            bool clean =
                _forceThermalVision.Value &&
                _cleanThermalVision.Value;
            thermal.IsNoisy =
                clean ? false : _originalThermalNoise;
            thermal.IsFpsStuck =
                clean ? false : _originalThermalFpsStuck;
            thermal.IsMotionBlurred =
                clean ? false : _originalThermalMotionBlur;
            thermal.IsGlitch =
                clean ? false : _originalThermalGlitch;
            thermal.IsPixelated =
                clean ? false : _originalThermalPixelation;
        }

        private void RestoreThermalImageSettings()
        {
            if (_configuredThermalVision == null)
                return;

            _configuredThermalVision.IsNoisy =
                _originalThermalNoise;
            _configuredThermalVision.IsFpsStuck =
                _originalThermalFpsStuck;
            _configuredThermalVision.IsMotionBlurred =
                _originalThermalMotionBlur;
            _configuredThermalVision.IsGlitch =
                _originalThermalGlitch;
            _configuredThermalVision.IsPixelated =
                _originalThermalPixelation;
        }

        private void CaptureNightVisionSettings(
            BSG.CameraEffects.NightVision nightVision)
        {
            if (nightVision == null ||
                ReferenceEquals(
                    nightVision,
                    _configuredNightVision))
            {
                return;
            }

            RestoreNightVisionImageSettings();
            _configuredNightVision = nightVision;
            _originalNightVisionOn = nightVision.On;
            _originalNightVisionNoiseIntensity =
                nightVision.NoiseIntensity;
        }

        private void ApplyNightVisionImageSettings(
            BSG.CameraEffects.NightVision nightVision)
        {
            if (nightVision == null)
                return;

            nightVision.NoiseIntensity =
                _forceNightVision.Value &&
                _cleanNightVision.Value
                    ? 0f
                    : _originalNightVisionNoiseIntensity;
        }

        private void RestoreNightVisionImageSettings()
        {
            if (_configuredNightVision != null)
            {
                _configuredNightVision.NoiseIntensity =
                    _originalNightVisionNoiseIntensity;
            }
        }

        private void CaptureNightVisionBloom(
            CameraClass camera)
        {
            UltimateBloom bloom =
                camera.Camera == null
                    ? null
                    : camera.Camera.GetComponent<UltimateBloom>();
            if (bloom == null ||
                ReferenceEquals(
                    bloom,
                    _configuredNightVisionBloom))
            {
                return;
            }

            RestoreNightVisionBloom();
            _configuredNightVisionBloom = bloom;
            _originalNightVisionBloomIntensity =
                bloom.m_BloomIntensity;
        }

        private void ApplyNightVisionBloom()
        {
            if (_configuredNightVisionBloom == null)
                return;

            _configuredNightVisionBloom.m_BloomIntensity =
                _forceNightVision.Value
                    ? _originalNightVisionBloomIntensity *
                        _nightVisionBloomAmount.Value
                    : _originalNightVisionBloomIntensity;
        }

        private void RestoreNightVisionBloom()
        {
            if (_configuredNightVisionBloom != null)
            {
                _configuredNightVisionBloom.m_BloomIntensity =
                    _originalNightVisionBloomIntensity;
            }
        }

        private void UpdateForcedVisorOverlay(
            CameraClass camera)
        {
            VisorEffect visor = camera.VisorEffect;
            CaptureVisorSettings(visor);
            if (visor == null)
                return;

            switch (_forcedVisorMode.Value)
            {
                case ForcedVisorMode.None:
                    visor.Visible = false;
                    break;
                case ForcedVisorMode.Narrow:
                    visor.SetMask(VisorEffect.EMask.Narrow);
                    visor.Visible = true;
                    visor.SetIntensity(
                        Mathf.Max(1f, visor.Intensity));
                    break;
                case ForcedVisorMode.Wide:
                    visor.SetMask(VisorEffect.EMask.Wide);
                    visor.Visible = true;
                    visor.SetIntensity(
                        Mathf.Max(1f, visor.Intensity));
                    break;
                default:
                    RestoreVisorSettings();
                    break;
            }
        }

        private void CaptureVisorSettings(
            VisorEffect visor)
        {
            if (visor == null ||
                ReferenceEquals(visor, _configuredVisorEffect))
            {
                return;
            }

            RestoreVisorSettings();
            _configuredVisorEffect = visor;
            _originalVisorVisible = visor.Visible;
            _originalVisorIntensity = visor.Intensity;
            _originalVisorMask = GetEquippedVisorMask();
        }

        private VisorEffect.EMask GetEquippedVisorMask()
        {
            EFT.InventoryLogic.FaceShieldComponent faceShield =
                _localPlayer == null ||
                _localPlayer.FaceShieldObserver == null
                    ? null
                    : _localPlayer.FaceShieldObserver.Component;
            return faceShield == null
                ? VisorEffect.EMask.NoMask
                : (VisorEffect.EMask)(int)faceShield.Mask;
        }

        private void RestoreVisorSettings()
        {
            if (_configuredVisorEffect == null)
                return;

            _configuredVisorEffect.SetMask(
                _originalVisorMask);
            _configuredVisorEffect.SetIntensity(
                _originalVisorIntensity);
            _configuredVisorEffect.Visible =
                _originalVisorVisible;
        }

        private void RestoreForcedVisionOverrides()
        {
            if (_configuredThermalVision != null)
                _configuredThermalVision.On = _originalThermalOn;
            if (_configuredNightVision != null)
                _configuredNightVision.On = _originalNightVisionOn;
            RestoreThermalImageSettings();
            RestoreNightVisionImageSettings();
            RestoreNightVisionBloom();
            RestoreVisorSettings();
            _configuredThermalVision = null;
            _configuredNightVision = null;
            _configuredNightVisionBloom = null;
            _configuredVisorEffect = null;
            _visionOverridesNeedUpdate = false;
        }
    }
}
