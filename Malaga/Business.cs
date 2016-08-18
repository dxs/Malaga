using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace Malaga
{
	/// <summary>
	/// 
	/// </summary>
	public class Business
	{
		/// <summary>
		///
		/// </summary>
		public string ID;
		/// <summary>
		/// 
		/// </summary>
		public string Name;
		/// <summary>
		/// 
		/// </summary>
		public string Description;
		/// <summary>
		/// 
		/// </summary>
		public double Distance;
		/// <summary>
		/// 
		/// </summary>
		public string PhotoUrl;
		/// <summary>
		/// 
		/// </summary>
		public double Latitude;
		/// <summary>
		/// 
		/// </summary>
		public double Longitude;
		/// <summary>
		/// 
		/// </summary>
		public string Rating;

		/// <summary>
		/// 
		/// </summary>
		public string ThumbnailUrl;

		internal async static Task<List<Business>> SaveAndTransformImage(List<Business> selectedItems)
		{
			Business business;
			string path = ApplicationData.Current.LocalFolder.Path;
			StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Photos", CreationCollisionOption.OpenIfExists);
			StorageFile file, thumbnail;

			for (int i = 0; i < selectedItems.Count; i++)
			{
				business = selectedItems[i];
				if (!business.PhotoUrl.Contains("http"))
					continue;
				try
				{
					HttpClient client = new HttpClient();
					byte[] buffer = await client.GetByteArrayAsync(business.PhotoUrl);
					file = await folder.CreateFileAsync(business.ID+".jpg", CreationCollisionOption.ReplaceExisting);
					using (Stream stream = await file.OpenStreamForWriteAsync())
						stream.Write(buffer, 0, buffer.Length);
					business.PhotoUrl = file.Path;
					thumbnail = await folder.CreateFileAsync("Thumbnail-" + business.ID + ".jpg", CreationCollisionOption.ReplaceExisting);
					CreateThumbnail(file, thumbnail, folder);
					business.ThumbnailUrl = thumbnail.Path;
				}
				catch (Exception e)
				{
					e.ToString();
				}

				selectedItems[i] = business;
			}
			return selectedItems;
		}

		private async static void CreateThumbnail(StorageFile source, StorageFile thumbnail, StorageFolder folder)
		{
			var minimum = 40.0;
			var imageStream = await source.OpenReadAsync();
			BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);
			uint originalPixelWidth = decoder.PixelWidth;
			uint originalPixelHeight = decoder.PixelHeight;

			using (imageStream)
			{
				//Resize if needed
				if (originalPixelHeight > minimum && originalPixelWidth > minimum)
				{
					using (IRandomAccessStream resizedStream = await thumbnail.OpenAsync(FileAccessMode.ReadWrite))
					{
						//Create encoder based on decoder 
						BitmapEncoder encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
						double widthRatio = (double)minimum / originalPixelWidth;
						double heightRatio = (double)minimum / originalPixelHeight;
						uint aspectHeight = (uint)minimum;
						uint aspectWidth = (uint)minimum;
						uint cropX = 0, cropY = 0;

						var scaledSize = (uint)minimum;
						if (originalPixelWidth > originalPixelHeight)
						{
							aspectWidth = (uint)(heightRatio * originalPixelWidth);
							cropX = (aspectWidth - aspectHeight) / 2;
						}
						else
						{
							aspectHeight = (uint)(widthRatio * originalPixelHeight);
							cropY = (aspectHeight - aspectWidth) / 2;
						}

						encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
						encoder.BitmapTransform.ScaledHeight = aspectHeight;
						encoder.BitmapTransform.ScaledWidth = aspectWidth;
						encoder.BitmapTransform.Bounds = new BitmapBounds()
						{
							Width = scaledSize,
							Height = scaledSize,
							X = cropX,
							Y = cropY,
						};

						await encoder.FlushAsync();
					}
				}
				else
					await source.CopyAndReplaceAsync(thumbnail);
			}
		}

		internal static string RandomId()
		{
			Random rand = new Random();
			string value = string.Empty;
			for(int i = 0; i < 30; i++)
			{
				value += rand.Next(9);
			}
			return value;
		}
	}
}
