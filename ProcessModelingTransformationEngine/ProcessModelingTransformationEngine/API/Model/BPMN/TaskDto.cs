namespace SEA_Models.Domain.Model.BPMN;

public class TaskDto : IBpmnElementDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}