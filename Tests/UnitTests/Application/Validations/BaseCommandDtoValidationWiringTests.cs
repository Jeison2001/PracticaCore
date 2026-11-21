using Application.Shared.Commands;
using Application.Shared.DTOs.AcademicPeriods;
using Application.Shared.DTOs.Faculties;
using Application.Shared.DTOs.IdentificationTypes;
using Application.Shared.DTOs.Roles;
using Application.Shared.DTOs.StudyPlans;
using Application.Shared.DTOs.Users;
using Application.Validations.BaseValidators;
using Application.Validations.SpecificValidators.AcademicPeriods;
using Application.Validations.SpecificValidators.Faculties;
using Application.Validations.SpecificValidators.IdentificationTypes;
using Application.Validations.SpecificValidators.Roles;
using Application.Validations.SpecificValidators.StudyPlans;
using Application.Validations.SpecificValidators.Users;
using Domain.Common.Users;
using Domain.Entities;
using FluentValidation;
using Xunit;

namespace Tests.UnitTests.Application.Validations
{
    public class BaseCommandDtoValidationWiringTests
    {
        [Fact]
        public void BaseCreateCommandValidator_Should_Invoke_UserValidator_And_Allow_NonStudent_Without_StudyPlan()
        {
            var validator = new BaseCreateCommandValidator<User, UserDto, int>(
                new IValidator<UserDto>[] { new UserValidator() });

            var teacherCmd = new CreateEntityCommand<User, int, UserDto>(
                new UserDto
                {
                    IdIdentificationType = 1,
                    Identification = "1065123456",
                    Email = "docente@unicesar.edu.co",
                    FirstName = "Docente",
                    LastName = "Prueba",
                    IdAcademicProgram = 1,
                    IdStudyPlan = null,
                    CumulativeAverage = null,
                    ApprovedCredits = null,
                    TotalAcademicCredits = null
                },
                new CurrentUserInfo());

            Assert.True(validator.Validate(teacherCmd).IsValid);

            var invalidCmd = new CreateEntityCommand<User, int, UserDto>(
                teacherCmd.Dto with
                {
                    Identification = new string('1', 21), // Excede VARCHAR(20)
                    CumulativeAverage = 5.5
                },
                new CurrentUserInfo());

            var result = validator.Validate(invalidCmd);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.Identification");
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.CumulativeAverage");
        }

        [Fact]
        public void BaseUpdateCommandValidator_Should_Invoke_RoleValidator_And_Allow_Long_System_Codes()
        {
            var validator = new BaseUpdateCommandValidator<Role, RoleDto, int>(
                new IValidator<RoleDto>[] { new RoleValidator() });

            var cmd = new UpdateEntityCommand<Role, int, RoleDto>(
                1,
                new RoleDto
                {
                    Id = 1,
                    Code = "PROGRAM_CURRICULUM_COMMITTEE", // 28 chars, valid in VARCHAR(50)
                    Name = "Comité Curricular",
                    Description = "Miembro del comité"
                },
                new CurrentUserInfo());

            Assert.True(validator.Validate(cmd).IsValid);

            var invalidCmd = new UpdateEntityCommand<Role, int, RoleDto>(
                1,
                cmd.Dto with { Code = new string('R', 51) },
                new CurrentUserInfo());

            var result = validator.Validate(invalidCmd);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == "Dto.Code");
        }

        [Fact]
        public void BaseCreateEntitiesCommandValidator_Should_Validate_Each_Item_With_AcademicPeriodValidator()
        {
            var validator = new BaseCreateEntitiesCommandValidator<AcademicPeriod, AcademicPeriodDto, int>(
                new IValidator<AcademicPeriodDto>[] { new AcademicPeriodValidator() });

            var emptyCmd = new CreateEntitiesCommand<AcademicPeriod, int, AcademicPeriodDto>(
                new List<AcademicPeriodDto>(),
                new CurrentUserInfo());
            Assert.False(validator.Validate(emptyCmd).IsValid);

            var invalidCmd = new CreateEntitiesCommand<AcademicPeriod, int, AcademicPeriodDto>(
                new List<AcademicPeriodDto>
                {
                    new()
                    {
                        Code = "2026-1",
                        StartDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                        EndDate = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)
                    }
                },
                new CurrentUserInfo());

            var result = validator.Validate(invalidCmd);
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName.Contains("EndDate"));
        }

        [Fact]
        public void StudyPlan_IdentificationType_And_Faculty_Validators_Match_Sql_Contracts()
        {
            var studyPlanValidator = new StudyPlanValidator();
            Assert.False(studyPlanValidator.Validate(new StudyPlanDto
            {
                Code = "SP-01",
                IdAcademicProgram = 1,
                StartYear = 2026,
                EndYear = 2024
            }).IsValid);

            var idTypeValidator = new IdentificationTypeValidator();
            Assert.False(idTypeValidator.Validate(new IdentificationTypeDto
            {
                Code = "ABCDE", // > VARCHAR(4)
                Name = "Documento"
            }).IsValid);

            var facultyValidator = new FacultyValidator();
            Assert.False(facultyValidator.Validate(new FacultyDto
            {
                Code = "",
                Name = "Ingeniería"
            }).IsValid);
        }
    }
}
