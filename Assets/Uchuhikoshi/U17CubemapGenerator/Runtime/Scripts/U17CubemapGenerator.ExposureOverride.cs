using System;
using UnityEngine;
using UnityEngine.Rendering;
#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
#endif

#nullable enable

namespace Uchuhikoshi.U17CubemapGenerator
{
	public partial class U17CubemapGenerator : MonoBehaviour, IU17CubemapGenerator
	{
#if USING_HDRP
		ExposureOverrideWork? _exposureOverrideWork;

		void StartExposureOverride(float fixedExposure, float compensation)
		{
			_exposureOverrideWork = new ExposureOverrideWork(fixedExposure, compensation);
		}

		void CleanupExposureOverride()
		{
			_exposureOverrideWork?.Dispose();
			_exposureOverrideWork = null;
		}

		class ExposureOverrideWork : IDisposable
		{
			Exposure? _exposureVolume;
			bool _volumeActiveOld;
			bool _exposureModeOverrideStateOld;
			ExposureMode _exposureModeOld;
			bool _fixedExposureOverrideStateOld;
			float _fixedExposureOld;
			bool _compensationOverrideStateOld;
			float _compensationOld;

			Volume? _volume;
			bool _added;

			public ExposureOverrideWork(float fixedExposure, float compensation)
			{
				FindOrAddExposureInScenes();
				if (_exposureVolume != null)
				{
					_volumeActiveOld = _exposureVolume.active;
					_exposureModeOverrideStateOld = _exposureVolume.mode.overrideState;
					_exposureModeOld = _exposureVolume.mode.value;
					_fixedExposureOverrideStateOld = _exposureVolume.fixedExposure.overrideState;
					_fixedExposureOld = _exposureVolume.fixedExposure.value;
					_compensationOverrideStateOld = _exposureVolume.compensation.overrideState;
					_compensationOld = _exposureVolume.compensation.value;

					_exposureVolume.active = true;
					_exposureVolume.mode.overrideState = true;
					_exposureVolume.mode.value = ExposureMode.Fixed;
					_exposureVolume.fixedExposure.overrideState = true;
					_exposureVolume.fixedExposure.value = fixedExposure;
					_exposureVolume.compensation.overrideState = true;
					_exposureVolume.compensation.value = compensation;
				}
			}

			public void Dispose()
			{
				if (_added)
				{
					_volume!.profile.Remove<Exposure>();
					CoreUtils.Destroy(_exposureVolume);
					_exposureVolume = null;
				}
				if (_exposureVolume != null)
				{
					_exposureVolume.active = _volumeActiveOld;
					_exposureVolume.mode.overrideState = _exposureModeOverrideStateOld;
					_exposureVolume.mode.value = _exposureModeOld;
					_exposureVolume.fixedExposure.overrideState = _fixedExposureOverrideStateOld;
					_exposureVolume.fixedExposure.value = _fixedExposureOld;
					_exposureVolume.compensation.overrideState = _compensationOverrideStateOld;
					_exposureVolume.compensation.value = _compensationOld;
					_exposureVolume = null;
				}
				_volume = null;
				_added = false;
			}

			void FindOrAddExposureInScenes()
			{
				var scene = SceneManager.GetActiveScene();
				var rootGameObjects = scene.GetRootGameObjects();
				for (int i = 0; i < rootGameObjects.Length; i++)
				{
					var volume = rootGameObjects[i].GetComponentInChildren<Volume>();
					if (volume != null)
					{
						if (volume.profile.TryGet<Exposure>(out _exposureVolume))
						{
							_volume = volume;
							break;
						}
						_volume = volume;
						_exposureVolume = _volume.profile.Add<Exposure>();
						_added = true;
						break;
					}
				}
			}
		}
#endif
	}
}