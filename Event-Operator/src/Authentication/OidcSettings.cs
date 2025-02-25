namespace FP.ContainerTraining.EventOperator.Authentication;

public class OidcSettings
{
    public required string Authority { get; set; }
    
    public required string ClientId { get; set; }
    
    public required string ClientSecret { get; set; }
    
    public required string AdminGroup { get; set; }
}