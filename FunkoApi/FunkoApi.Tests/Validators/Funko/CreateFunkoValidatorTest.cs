using FunkoApi.Dto.Funko;
using FunkoApi.Validators.Funko;

namespace FunkoApi.Tests.Validators.Funko;

/// <summary>Tests de CreateFunkoValidator.</summary>
public abstract class CreateFunkoValidatorTest
{
    protected CreateFunkoValidator _validator = null!;
    protected CreateFunkoDto _dtoValido = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateFunkoValidator();
        _dtoValido = new CreateFunkoDto(
            Nombre: "Funko Pop Batman",
            Precio: 19.99m,
            Stock: 10,
            CategoriaId: "dQw4w9WgXcQ",
            ProveedorId: "507f1f77bcf86cd799439011",
            Imagen: "https://example.com/batman.png");
    }

    [TestFixture]
    public class CasosValidos : CreateFunkoValidatorTest
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

        [TestCase(0.01)]                  // mínimo estricto (> 0)
        [TestCase(999999.99)]
        public void Validar_PrecioMayorQueCero_NoTieneErrores(decimal precio)
        {
            // Arrange
            var dto = _dtoValido with { Precio = precio };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Precio);
        }

        [TestCase(0)]
        [TestCase(9999)]
        public void Validar_StockPositivo_NoTieneErrores(int stock)
        {
            // Arrange
            var dto = _dtoValido with { Stock = stock };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Stock);
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validar_ImagenVaciaONula_NoTieneErrores(string? imagen)
        {
            // Arrange
            var dto = _dtoValido with { Imagen = imagen };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
        }

        [TestCase("https://example.com/imagen.png")]
        [TestCase("http://example.com/imagen.jpg")]
        [TestCase("/imagenes/funko.png")]              // ruta relativa
        [TestCase("imagen.png")]                       // relativa simple
        public void Validar_ImagenConUrlValida_NoTieneErrores(string imagen)
        {
            // Arrange
            var dto = _dtoValido with { Imagen = imagen };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
        }
    }

    [TestFixture]
    public class CasosInvalidos : CreateFunkoValidatorTest
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
                  .WithErrorMessage("El nombre del Funko es obligatorio.");
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

        [Test]
        public void Validar_PrecioCero_TieneErrorNotEmpty()
        {
            // Arrange
            var dto = _dtoValido with { Precio = 0m };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Precio)
                  .WithErrorMessage("El precio del Funko es obligatorio.");
        }

        [TestCase(-1)]
        [TestCase(-99.99)]
        public void Validar_PrecioNegativo_TieneErrorGreaterThan(decimal precio)
        {
            // Arrange
            var dto = _dtoValido with { Precio = precio };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Precio)
                  .WithErrorMessage("El precio debe ser mayor que 0.");
        }

        [TestCase(-1)]
        public void Validar_StockNegativo_TieneErrorGreaterThanOrEqual(int stock)
        {
            // Arrange
            var dto = _dtoValido with { Stock = stock };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Stock)
                  .WithErrorMessage("El stock no puede ser negativo.");
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validar_CategoriaIdVacio_TieneErrorNotEmpty(string? categoriaId)
        {
            // Arrange
            var dto = _dtoValido with { CategoriaId = categoriaId! };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CategoriaId)
                  .WithErrorMessage("La categoría es obligatoria.");
        }

        [TestCase(null)]
        [TestCase("")]
        public void Validar_ProveedorIdVacio_TieneErrorNotEmpty(string? proveedorId)
        {
            // Arrange
            var dto = _dtoValido with { ProveedorId = proveedorId! };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProveedorId)
                  .WithErrorMessage("El proveedor es obligatorio.");
        }
    }
}