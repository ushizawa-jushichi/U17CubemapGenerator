using UnityEngine;

namespace Uchuhikoshi
{
    public struct CameraCacheParameter
    {
		public Vector3 position;
		public Quaternion rotation;

		public float fieldOfView;
		public float nearClipPlane;
		public float farClipPlane;
		public bool orthographic;
		public float orthographicSize;
		public Color backgroundColor;

		public bool usePhysicalProperties;
		public Vector2 sensorSize;
		public Vector2 lensShift;
		public float focalLength;
		public Camera.GateFitMode gateFit;

		public CameraCacheParameter(Camera other)
		{
			this.position = other.transform.position;
			this.rotation = other.transform.rotation;

			this.fieldOfView = other.fieldOfView;
			this.nearClipPlane = other.nearClipPlane;
			this.farClipPlane = other.farClipPlane;
			this.orthographic = other.orthographic;
			this.orthographicSize = other.orthographicSize;
			this.backgroundColor = other.backgroundColor;

			this.usePhysicalProperties = other.usePhysicalProperties;
			this.sensorSize = other.sensorSize;
			this.lensShift = other.lensShift;
			this.focalLength = other.focalLength;
			this.gateFit = other.gateFit;
		}

		public CameraCacheParameter(CameraCacheParameter other)
		{
			this.position = other.position;
			this.rotation = other.rotation;

			this.fieldOfView = other.fieldOfView;
			this.nearClipPlane = other.nearClipPlane;
			this.farClipPlane = other.farClipPlane;
			this.orthographic = other.orthographic;
			this.orthographicSize = other.orthographicSize;
			this.backgroundColor = other.backgroundColor;

			this.usePhysicalProperties = other.usePhysicalProperties;
			this.sensorSize = other.sensorSize;
			this.lensShift = other.lensShift;
			this.focalLength = other.focalLength;
			this.gateFit = other.gateFit;
		}

		public void CopyTo(Camera destination, bool withPositionAndRotation = true)
		{
			if (withPositionAndRotation)
			{
				destination.transform.position = this.position;
				destination.transform.rotation = this.rotation;
			}

			destination.fieldOfView = this.fieldOfView;
			destination.nearClipPlane = this.nearClipPlane;
			destination.farClipPlane = this.farClipPlane;
			destination.orthographic = this.orthographic;
			destination.orthographicSize = this.orthographicSize;
			destination.backgroundColor = this.backgroundColor;

			destination.usePhysicalProperties = this.usePhysicalProperties;
			destination.sensorSize = this.sensorSize;
			destination.lensShift = this.lensShift;
			destination.focalLength = this.focalLength;
			destination.gateFit = this.gateFit;
		}
	}
}
