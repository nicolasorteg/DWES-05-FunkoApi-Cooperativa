using FunkoApi.Dto.Proveedor;
using FunkoApi.Validators.Proveedor;

namespace FunkoApi.Tests.Validators.Proveedor;

/// <summary>Tests de UpdateProveedorValidator. </summary>
public abstract class UpdateProveedorValidatorTest
{
    protected UpdateProveedorValidator _validator = null!;
    protected UpdateProveedorDto _dtoValido = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateProveedorValidator();
        _dtoValido = new UpdateProveedorDto(
            Nombre: "Proveedor S.A.",
            Email: "contacto@proveedor.com",
            Telefono: "+34 600 123 456");
    }

    [TestFixture]
    public class CasosValidos : UpdateProveedorValidatorTest
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

        [TestCase(null)]
        [TestCase("")]
        public void Validar_TelefonoVacio_NoTieneErrores(string? telefonoVacio)
        {
            // Arrange
            var dto = _dtoValido with { Telefono = telefonoVacio };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
        }

        [TestCase("1234567")]
        [TestCase("12345678901234567890")]
        [TestCase("+34 600 123 456")]
        [TestCase("600 123 456")]
        [TestCase("(34) 600-123-456")]
        public void Validar_TelefonoConFormatoValido_NoTieneErrores(string telefono)
        {
            // Arrange
            var dto = _dtoValido with { Telefono = telefono };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Telefono);
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

        [Test]
        public void Validar_EmailCon150Caracteres_NoTieneErrores()
        {
            // Arrange
            var email = new string('a', 144) + "@b.com";
            var dto = _dtoValido with { Email = email };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [TestCase("espacio en@medio.com")]
        public void Validar_EmailAceptadoPorValidadorPorDefecto_NoTieneErrores(string email)
        {
            // Arrange
            var dto = _dtoValido with { Email = email };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Email);
        }
    }

    [TestFixture]
    public class CasosInvalidos : UpdateProveedorValidatorTest
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
                  .WithErrorMessage("El nombre del proveedor es obligatorio.");
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

        [TestCase(null)]
        [TestCase("")]
        public void Validar_EmailVacio_TieneErrorNotEmpty(string? email)
        {
            // Arrange
            var dto = _dtoValido with { Email = email! };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("El email del proveedor es obligatorio.");
        }

        [TestCase("no-es-email")]
        [TestCase("sin@")]
        [TestCase("@dominio.com")]
        public void Validar_EmailConFormatoInvalido_TieneErrorEmailAddress(string email)
        {
            // Arrange
            var dto = _dtoValido with { Email = email };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("El formato del email no es válido.");
        }

        [Test]
        public void Validar_EmailCon151Caracteres_TieneErrorMaximumLength()
        {
            // Arrange
            var email = new string('a', 145) + "@b.com"; // 151 chars
            var dto = _dtoValido with { Email = email };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("El email no puede superar los 150 caracteres.");
        }

        [TestCase("123456")]
        [TestCase("123456789012345678901")]
        [TestCase("abc123456")]
        [TestCase("++1234567")]
        [TestCase("123@456")]
        [TestCase("(+34) 600-123-456")]      
        public void Validar_TelefonoConFormatoInvalido_TieneErrorMatches(string telefono)
        {
            // Arrange
            var dto = _dtoValido with { Telefono = telefono };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Telefono)
                  .WithErrorMessage("El teléfono debe tener un formato válido (entre 7 y 20 caracteres, solo dígitos, espacios, +, -, paréntesis).");
        }
    }
}