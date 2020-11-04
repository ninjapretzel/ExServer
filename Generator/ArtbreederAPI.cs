using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Ex {
	public class ArtbreederAPI {

		/// <summary> Clamp <paramref name="v"/> between 0 and 1 </summary>
		/// <param name="v"> value to clamp </param>
		/// <returns> [ 0 ... v ... 1 ] </returns>
		public static double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
		/// <summary> Clamp <paramref name="v"/> between <paramref name="min"/> and <paramref name="max"/> </summary>
		/// <param name="v"> value to clamp </param>
		/// <param name="min"> minimum value to return </param>
		/// <param name="max"> maximum value to return </param>
		/// <returns> [ min ... v ... max ] </returns>
		public static double Clamp(double v, double min, double max) { return v < min ? min : v > max ? max : v; }

		/// <summary> Information about Artbreeder's available models. </summary>
		public static JsonObject models = Json.Parse(@"{
	anime_portraits:{
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -3, 3 ], },
		genes: {
			chaos,
			smile, open_mouth, blush,
			looking_at_viewer, 'close-up',
			bangs, long_hair, hair_between_eyes,
			black_hair, brown_hair, purple_hair,
			red_eyes, green_eyes, blue_eyes, purple_eyes, brown_eyes, 
		},
	},
	general: { 
		magnitudes: { chaos: [ .05, .9999 ], },
		genes: { chaos },
	},
	portraits_sg2: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -2, 2 ], },
		genes: {
			chaos,
			gender, width, age, height, yaw, pitch, art,
			red, green, blue, hue, saturation, brightness, sharpness,
			black, indian, asian, white, 'middle-eastern', 'latino-hispanic', 
			happy, angry, eyesopen, mouthopen, 
			earrings, makeup, eyeglasses, facial_hair, hat,
			blue_eyes, 
			black_hair, blonde_hair, brown_hair, 
		},
	},
	landscapes_sg2: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -1, 1 ], },
		genes: {
			chaos,
			red, green, blue, h, s, v, sharpness,
			art,
			plant, field, water, slope, building, rock, path, ground, 
			ruins, sunlight,
		}
	},
	sci_bio_art: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -2, 2 ], },
		genes: {
			chaos,
			geometry,
			futuristic, space, earth, 'city skyline',
			microbiology, organic, eye, 
		}
	},
	albums: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -6, 6 ], },
		genes: {
			chaos,
			red, green, blue,
			'Blur 1', 'Blur 2',
		}
	},
	furries: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -5, 5 ], },
		genes: {
			chaos,
			human, drawing, manga, wildlife,
			roll, yaw, smile, 
			red, green, blue, brightness, sharpness, h, s, v,
			'eyes-closed', 'open-mouth', 'teeth',
			
		}
	},
	characters: {
		magnitudes: { chaos: [ .1, 1.5 ], $rest: [ -8, 8 ], },
		genes: {
			chaos,
			red, green, blue, hue, saturation, color, sharpness,
			female, width, human, face,
			ninja, soldier,
			clothing, armor, coat, helmet, suit, jacket,
		},
	}
}".Replace('\'', '\"')) as JsonObject;
		/// <summary> Cookie to use when sending requests to Artbreeder. </summary>
		public static string cookie = "";

		public static readonly string baseURL = "https://artbreeder.com";
		public static readonly string tempComposeURL = baseURL + "/tmp_compose_images";

		/// <summary> Prepare a string into an HTTPContent when POSTing to Artbreeder </summary>
		/// <param name="content"> String to prepare </param>
		/// <returns> HTTP Content for request body </returns>
		public static HttpContent Prepare(string content) {
			HttpContent c = new StringContent(content, Encoding.UTF8, "application/json");
			c.Headers.Add("Cookie", cookie);
			return c;
		}

		/// <summary> Generate and save a new picture from the given model. </summary>
		/// <param name="model"> Model to generate with </param>
		public static void GenerateNew(string model) {
			if (!models.ContainsKey(model)) { return; }

			JsonObject payload = new JsonObject();
			payload["model_name"] = model;
			payload["parents"] = new JsonObject();
			payload["gene_values"] = new JsonObject("chaos", .7);
			payload["method"] = "mate";

			Request.Post(tempComposeURL, payload.ToString(), (id) => {
				//Save(model, result);
				Log.Info($"Generated new image ID {id} from {model}");
				id = id.Replace("\"", "");
				SaveImage(model, id);
				Save(model, id);
			}, Prepare);
		}

		/// <summary> Generate a child of a given image, within a given model. </summary>
		/// <param name="model"> Model to generate with </param>
		/// <param name="parent"> ID of parent </param>
		/// <param name="style"> Ratio of style to keep (0, 1) </param>
		/// <param name="content"> Ratio of content to keep (0, 1) </param>
		/// <param name="chaos"> Craziness parameter. Range depends on model. </param>
		public static void GenerateChild(string model, string parent, double style = .5f, double content = .5f, double chaos = .6f) {
			if (!models.ContainsKey(model)) { return; }
			JsonObject info = models[model] as JsonObject;
			JsonObject magnitudes = info["magnitudes"] as JsonObject;
			JsonArray chaosRange = magnitudes["chaos"] as JsonArray;
			
			chaos = Clamp(chaos, chaosRange[0].floatVal, chaosRange[1].floatVal);
			style = Clamp01(style);
			content = Clamp01(content);

			JsonObject payload = new JsonObject();
			payload["model_name"] = model;
			payload["parents"] = new JsonObject(parent, new JsonArray(style, content));
			payload["gene_values"] = new JsonObject("chaos", chaos);
			payload["method"] = "mate";

			Request.Post(tempComposeURL, payload.ToString(), (id) => {
				//Save(model, result);
				Log.Info($"Single child of {parent}, child is ID {id} from {model}");
				id = id.Replace("\"", "");
				SaveImage(model, id);
				Save(model, id, parent);
			}, Prepare);

		}

		/// <summary> Breed 2 images together </summary>
		/// <param name="model"> Model to use </param>
		/// <param name="parentA"> ID of first parent </param>
		/// <param name="parentB"> ID of second parent </param>
		/// <param name="styleA"> Ratio of style of first parent </param>
		/// <param name="contentA"> Ratio of content of first parent </param>
		/// <param name="styleB"> Ratio of style of second parent </param>
		/// <param name="contentB"> Ratio of content of second parent </param>
		/// <param name="chaos"> Craziness parameter </param>
		public static void Breed(string model, string parentA, string parentB,
				double styleA = .5f, double contentA = .5f, double styleB = .5f, double contentB = .5f, double chaos = .6f) {
			if (!models.ContainsKey(model)) { return; }

			JsonObject info = models[model] as JsonObject;
			JsonObject magnitudes = info["magnitudes"] as JsonObject;
			JsonArray chaosRange = magnitudes["chaos"] as JsonArray;

			chaos = Clamp(chaos, chaosRange[0].floatVal, chaosRange[1].floatVal);
			styleA = Clamp01(styleA);
			styleB = Clamp01(styleB);
			contentA = Clamp01(contentA);
			contentB = Clamp01(contentB);

			JsonObject payload = new JsonObject();
			payload["model_name"] = model;
			payload["parents"] = new JsonObject(parentA, new JsonArray(styleA, contentA),
												parentB, new JsonArray(styleB, contentB));
			payload["gene_values"] = new JsonObject("chaos", chaos);
			payload["method"] = "mate";

			Request.Post(tempComposeURL, payload.ToString(), (id)=>{

				Log.Info($"Bred {parentA} & {parentB}, child is ID {id} from {model}");
				id = id.Replace("\"", "");
				SaveImage(model, id);
				Save(model, id, parentA, parentB);
			}, Prepare);

		}

		/// <summary> Save an image from Artbreeder's temporary locations. </summary>
		/// <param name="model"> Model that holds the image (used to place it in a local folder) </param>
		/// <param name="id"> Image ID </param>
		public static void SaveImage(string model, string id) {
			string dir = $"./public/{model}";
			if (!Directory.Exists(dir)) {
				Directory.CreateDirectory(dir);
			}
			Request.GetRaw($"https://s3.amazonaws.com/artbreederpublic-shortlived/1d/imgs/{id}.jpeg", (result)=>{
				string filename = $"{dir}/{id}.jpeg";
				Log.Info($"Creating file {filename}");
				File.WriteAllBytes(filename, result);
			});
		}

		/// <summary> Tell art breeder to save an image for us. </summary>
		/// <param name="model"> Model that is being used </param>
		/// <param name="id"> ID of image to save </param>
		/// <param name="parent"> Optional parent to allow artbreeder to record history.</param>
		public static void Save(string model, string id, params string[] parentIDs) {
			if (!models.ContainsKey(model)) { return; }


			JsonObject payload = new JsonObject();
			JsonArray parents = new JsonArray();
			if (parentIDs != null && parentIDs.Length > 0) { 
				parents.AddAll(parentIDs);
			}
			payload["key"] = id;
			payload["parent_keys"] = parents;
			payload["model_name"] = model;
			
			Request.Post("https://artbreeder.com/save_tmp_image", payload.ToString(), (result) => {
				Log.Info($"Saved {id} @ {model}. Got {result}");
			}, Prepare);
			
		}

	}

}
