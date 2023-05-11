using Microsoft.ML;
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tg_bot_rec
{
    internal class Machine
    {
        public ITransformer model;
        public IDataView trainingDataView;
        public IDataView testDataView;
        public MLContext mlContext;

        public Machine()
        {
            mlContext = new MLContext();
            (trainingDataView, testDataView) = LoadData(mlContext);
            model = BuildAndTrainModel(mlContext, trainingDataView);

            EvaluateModel(mlContext, testDataView, model);
        }
        public (IDataView training, IDataView test) LoadData(MLContext mlContext)
        {
            var trainingDataPath = "C:\\Users\\м\\OneDrive\\Рабочий стол\\Универ\\2 курс\\2 семестр\\Practica\\tg_bot_rec\\tg_bot_rec\\data\\ratings.csv";//Path.Combine(Environment.CurrentDirectory, "Data", "ratings.csv");
            var testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "recommendation-ratings-test.csv");

            IDataView trainingDataView = mlContext.Data.LoadFromTextFile<MovieRating>(trainingDataPath, hasHeader: true, separatorChar: ',');
            IDataView testDataView = mlContext.Data.LoadFromTextFile<MovieRating>(testDataPath, hasHeader: true, separatorChar: ',');

            return (trainingDataView, testDataView);
        }

        public ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
        {
            IEstimator<ITransformer> estimator = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "userIdEncoded", inputColumnName: "userId")
    .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "movieIdEncoded", inputColumnName: "movieId"));

            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "userIdEncoded",
                MatrixRowIndexColumnName = "movieIdEncoded",
                LabelColumnName = "Label",
                NumberOfIterations = 21,
                ApproximationRank = 100
            };

            var trainerEstimator = estimator.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

            Console.WriteLine("=============== Training the model ===============");
            ITransformer model = trainerEstimator.Fit(trainingDataView);

            return model;
        }

        public void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
        {
            Console.WriteLine("=============== Evaluating the model ===============");
            var prediction = model.Transform(testDataView);

            var metrics = mlContext.Regression.Evaluate(prediction, labelColumnName: "Label", scoreColumnName: "Score");

            Console.WriteLine("Root Mean Squared Error : " + metrics.RootMeanSquaredError.ToString());
            Console.WriteLine("RSquared: " + metrics.RSquared.ToString());
        }

        public bool UseModelForSinglePrediction(MLContext mlContext, ITransformer model, float user_id, int idFilm)
        {
            if (mlContext != null && model != null)
            {
                Console.WriteLine("=============== Making a prediction ===============");
                var predictionEngine = mlContext.Model.CreatePredictionEngine<MovieRating, MovieRatingPrediction>(model);

                var testInput = new MovieRating { userId = user_id, movieId = idFilm };

                var movieRatingPrediction = predictionEngine.Predict(testInput);

                Console.WriteLine(movieRatingPrediction.Score);
                if (Math.Round(movieRatingPrediction.Score, 1) > 3.5)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
