using FunkoApi.Dto.Categoria;
using FunkoApi.Validators.Categoria;

namespace FunkoApi.Tests.Validators.Categoria;

/// <summary>Tests de CreateCategoriaValidator.</summary>
public abstract class CreateCategoriaValidatorTest
{
    protected CreateCategoriaValidator _validator = null!;
    protected CreateCategoriaDto _dtoValido = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateCategoriaValidator();
        _dtoValido = new CreateCategoriaDto(
            Nombre: "Acción");
    }

    [TestFixture]
    public class CasosValidos : CreateCategoriaValidatorTest
    {
        [Test]
        public void Validar_DtoValido_NoTieneErrores()
        {
            // Arrange

            // Act
            var result = _validator.TestValidate(_dtoValido);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validar_NombreCon100Caracteres_NoTieneErrores()
        {
            // Arrange
            var dto = _dtoValido with { Nombre = new string('a', 100) };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
        }

        [TestCase("A")]
        [TestCase("Categoría con espacios")]
        [TestCase("Categoría con ñ y acentos áéíóú")]
        public void Validar_NombreConFormatoValido_NoTieneErrores(string nombre)
        {
            // Arrange
            var dto = _dtoValido with { Nombre = nombre };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Nombre);
        }
    }

    [TestFixture]
    public class CasosInvalidos : CreateCategoriaValidatorTest
    {
        [TestCase(null)]
        [TestCase("")]
        public void Validar_NombreVacio_TieneErrorNotEmpty(string? nombre)
        {
            // Arrange
            var dto = _dtoValido with { Nombre = nombre! };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre)
                  .WithErrorMessage("El nombre de la categoría es obligatorio.");
        }

        [Test]
        public void Validar_NombreCon101Caracteres_TieneErrorMaximumLength()
        {
            // Arrange
            var dto = _dtoValido with { Nombre = new string('a', 101) };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre)
                  .WithErrorMessage("El nombre no puede superar los 100 caracteres.");
        }
    }
}