namespace InnoHub.ModelDTO.ML
{
    public class FlaskEndpoints
    {
        public string Health { get; set; } = "/models-state";
        public string Recommend { get; set; } = "/recommend";
        public string SpamDetection { get; set; } = "/detect-spam";
        public string SalesPrediction { get; set; } = "/predict-sales";
    }
}
