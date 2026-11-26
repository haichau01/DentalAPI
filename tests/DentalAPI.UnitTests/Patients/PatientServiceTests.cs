using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Patients;
using DentalAPI.Domain.Patients;
using DentalAPI.UnitTests.TestDoubles;
using FluentAssertions;
using FluentValidation;

namespace DentalAPI.UnitTests.Patients;

public sealed class PatientServiceTests
{
    private readonly InMemoryPatientRepository _repository = new();
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _service = new PatientService(
            _repository,
            _repository,
            new SavePatientRequestValidator(new FixedTimeProvider(PatientTestData.UtcNow)),
            new PageQueryValidator());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_AddsPatientAndSaves()
    {
        var result = await _service.CreateAsync(PatientTestData.ValidRequest(), CancellationToken.None);

        _repository.Patients.Should().ContainSingle(patient => patient.Id == result.Id);
        _repository.SaveChangesCallCount.Should().Be(1);
        result.FullName.Should().Be("Nguyen Van A");
    }

    [Fact]
    public async Task CreateAsync_InvalidRequest_ThrowsValidationExceptionAndDoesNotSave()
    {
        var request = PatientTestData.ValidRequest() with { FullName = string.Empty };

        var act = () => _service.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Patients.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_PatientDoesNotExist_ThrowsNotFoundException()
    {
        var act = () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_PatientExists_UpdatesDetails()
    {
        var patient = AddExistingPatient();
        var request = PatientTestData.ValidRequest() with { PhoneNumber = "0911111111" };

        var result = await _service.UpdateAsync(patient.Id, request, CancellationToken.None);

        result.PhoneNumber.Should().Be("0911111111");
        patient.PhoneNumber.Should().Be("0911111111");
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_PatientExists_RemovesAndSaves()
    {
        var patient = AddExistingPatient();

        await _service.DeleteAsync(patient.Id, CancellationToken.None);

        _repository.Patients.Should().BeEmpty();
        _repository.SaveChangesCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, PageQuery.MaxPageSize + 1)]
    public async Task ListAsync_PageOutOfRange_ThrowsValidationException(int page, int pageSize)
    {
        var query = new PageQuery { Page = page, PageSize = pageSize };

        var act = () => _service.ListAsync(query, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private Patient AddExistingPatient()
    {
        var patient = new Patient(PatientTestData.ValidRequest().ToDetails());
        _repository.Add(patient);
        return patient;
    }
}
