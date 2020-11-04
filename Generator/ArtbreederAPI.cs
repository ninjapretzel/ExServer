using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Ex {
	public class ArtbreederAPI {


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
		public static string cookie = "";

		public static HttpContent Prepare(string content) {
			HttpContent c = new StringContent(content, Encoding.UTF8, "application/json");
			c.Headers.Add("Cookie", cookie);
			return c;
		}


		public static void GenerateNew(string model) {
			if (!models.ContainsKey(model)) { return; }

			JsonObject payload = new JsonObject();
			payload["model_name"] = model;
			payload["parents"] = new JsonObject();
			payload["gene_values"] = new JsonObject();
			payload["method"] = "mate";

			Request.Post("https://artbreeder.com/tmp_compose_images", payload.ToString(), (id) => {
				//Save(model, result);
				Log.Info($"Got ID {id} from {model}");
				id = id.Replace("\"", "");
				SaveImage(model, id);
				Save(model, id);
			}, Prepare);

		}

		public static void GenerateChild(string model, string parent, float style = .5f, float content = .5f, float chaos = .6f) {
			if (!models.ContainsKey(model)) { return; }

			JsonObject payload = new JsonObject();
			payload["model_name"] = model;
			payload["parents"] = new JsonObject(parent, new JsonArray(style, content));
			payload["gene_values"] = new JsonObject("chaos", chaos);
			payload["method"] = "mate";

			Request.Post("https://artbreeder.com/tmp_compose_images", payload.ToString(), (id) => {
				//Save(model, result);
				Log.Info($"Got ID {id} from {model}");
				id = id.Replace("\"", "");
				SaveImage(model, id);
				Save(model, id, parent);
			}, Prepare);

		}


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


		public static void Save(string model, string id, string parent = null) {
			if (!models.ContainsKey(model)) { return; }

			JsonObject payload = new JsonObject();
			JsonArray parents = new JsonArray();
			if (parent != null) { parents.Add(parent); }
			payload["key"] = id;
			payload["parent_keys"] = parents;
			payload["model_name"] = model;
			
			Request.Post("https://artbreeder.com/save_tmp_image", payload.ToString(), (result) => {
				Log.Info($"Saved {id} @ {model}. Got {result}");
			}, Prepare);
			
		}

	}

}
