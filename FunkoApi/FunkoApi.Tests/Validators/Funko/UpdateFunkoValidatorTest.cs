using FunkoApi.Dto.Funko;
using FunkoApi.Validators.Funko;

namespace FunkoApi.Tests.Validators.Funko;

/// <summary>Tests de UpdateFunkoValidator.</summary>
public abstract class UpdateFunkoValidatorTest
{
    protected UpdateFunkoValidator _validator = null!;
    protected UpdateFunkoDto _dtoValido = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UpdateFunkoValidator();
        _dtoValido = new UpdateFunkoDto(
            Nombre: "Funko Pop Batman",
            Precio: 19.99m,
            Stock: 10,
            CategoriaId: "dQw4w9WgXcQ",
            ProveedorId: "507f1f77bcf86cd799439011",
            Imagen: "https://example.com/batman.png");
    }

    [TestFixture]
    public class CasosValidos : UpdateFunkoValidatorTest
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
        public void Validar_DtoConTodosLosCamposNulos_NoTieneErrores()
        {
            // Arrange
            var dto = new UpdateFunkoDto(
                Nombre: null, Precio: null, Stock: null,
                CategoriaId: null, ProveedorId: null, Imagen: null);

            // Act
            var result = _validator.TestValidate(dto);

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

        [TestCase(0.01)]
        [TestCase(19.99)]
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
        [TestCase(1)]
        [TestCase(9999)]
        public void Validar_StockNoNegativo_NoTieneErrores(int stock)
        {
            // Arrange
            var dto = _dtoValido with { Stock = stock };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Stock);
        }

        [TestCase("https://example.com/imagen.png")]
        [TestCase("http://example.com/imagen.jpg")]
        [TestCase("/imagenes/funko.png")]
        [TestCase("imagen.png")]
        public void Validar_ImagenConUrlValida_NoTieneErrores(string imagen)
        {
            // Arrange
            var dto = _dtoValido with { Imagen = imagen };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.Imagen);
        }

        [TestCase("dQw4w9WgXcQ")]
        [TestCase("507f1f77bcf86cd799439011")]
        public void Validar_CategoriaYProveedorConValor_NoTieneErrores(string id)
        {
            // Arrange
            var dto = _dtoValido with { CategoriaId = id, ProveedorId = id };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.CategoriaId);
            result.ShouldNotHaveValidationErrorFor(x => x.ProveedorId);
        }
    }

    [TestFixture]
    public class CasosInvalidos : UpdateFunkoValidatorTest
    {
        [Test]
        public void Validar_NombreVacio_TieneErrorNotEmpty()
        {
            // Arrange
            var dto = _dtoValido with { Nombre = "" };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Nombre)
                  .WithErrorMessage("El nombre del Funko no puede estar vacío si se proporciona");
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

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-99.99)]
        public void Validar_PrecioNoPositivo_TieneErrorGreaterThan(decimal precio)
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
        [TestCase(-100)]
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

        [Test]
        public void Validar_CategoriaIdVacia_TieneErrorNotEmpty()
        {
            // Arrange
            var dto = _dtoValido with { CategoriaId = "" };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.CategoriaId)
                  .WithErrorMessage("La categoría no puede estar vacía si se proporciona.");
        }

        [Test]
        public void Validar_ProveedorIdVacio_TieneErrorNotEmpty()
        {
            // Arrange
            var dto = _dtoValido with { ProveedorId = "" };

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ProveedorId)
                  .WithErrorMessage("El proveedor no puede estar vacío si se proporciona.");
        }
    }
}