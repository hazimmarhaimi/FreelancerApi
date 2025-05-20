using System.Collections.Generic;

namespace FreelancerAPI.Dtos;

public record FreelancerDto(int Id, string Username, string Email, string PhoneNumber, bool IsArchived, List<string> Skillsets, List<string> Hobbies);

public record CreateFreelancerDto(string Username, string Email, string PhoneNumber, List<string> Skillsets, List<string> Hobbies);

public record UpdateFreelancerDto(string Username, string Email, string PhoneNumber, List<string> Skillsets, List<string> Hobbies);