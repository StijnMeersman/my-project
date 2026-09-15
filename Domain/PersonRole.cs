namespace my_project.Domain;

/// <summary>
/// What a person is allowed to set up. Not a hierarchy: a manager may also be assigned to projects
/// and log hours (spec 001 §5.4).
/// </summary>
public enum PersonRole
{
    Employee = 0,
    Manager = 1,
}
