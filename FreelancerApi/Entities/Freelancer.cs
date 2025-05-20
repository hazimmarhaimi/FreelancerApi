using System.Collections.Generic;

namespace FreelancerAPI.Enitites;

public class Freelancer { 
    public int Id { get; set; } 
    public string Username { get; set; } = string.Empty; 
    public string Email { get; set; } = string.Empty; 
    public string PhoneNumber { get; set; } = string.Empty; 
    public bool IsArchived { get; set; } 
    public List<Skillset> Skillsets { get; set; } = new(); 
    public List<Hobby> Hobbies { get; set; } = new(); 
}

public class Skillset { 
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty; 
    public int FreelancerId { get; set; }
    public Freelancer Freelancer { get; set; } = null!; 
}

public class Hobby { 
    public int Id { get; set; } 
    public string Name { get; set; } = string.Empty; 
    public int FreelancerId { get; set; } 
    public Freelancer Freelancer { get; set; } = null!; 
}