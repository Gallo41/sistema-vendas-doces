// ========================================
// 🍰 Sistema de Vendas de Doces - JavaScript Global
// ========================================

document.addEventListener('DOMContentLoaded', function () {

    // ========================================
    // 1. CONVERSÃO VÍRGULA → PONTO NO SUBMIT
    // Para campos com inputmode="decimal", troca vírgula por ponto antes de enviar
    // ========================================
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('input[inputmode="decimal"], input[data-validate="number"]').forEach(function (input) {
                if (input.value) {
                    input.value = input.value.replace(',', '.');
                }
            });
        });
    });

    // ========================================
    // 2. MÁSCARA DE TELEFONE - (XX) XXXXX-XXXX
    // Aplica em inputs com data-mask="phone"
    // ========================================
    document.querySelectorAll('input[data-mask="phone"]').forEach(function (input) {
        input.addEventListener('input', function (e) {
            var value = e.target.value.replace(/\D/g, ''); // Remove tudo que não é dígito
            if (value.length > 11) value = value.substring(0, 11);

            if (value.length > 6) {
                value = '(' + value.substring(0, 2) + ') ' + value.substring(2, 7) + '-' + value.substring(7);
            } else if (value.length > 2) {
                value = '(' + value.substring(0, 2) + ') ' + value.substring(2);
            } else if (value.length > 0) {
                value = '(' + value;
            }
            e.target.value = value;
        });

        // Bloquear letras no telefone
        input.addEventListener('keypress', function (e) {
            var char = String.fromCharCode(e.which || e.keyCode);
            if (!/[\d\(\)\-\s\+]/.test(char) && e.which !== 8 && e.which !== 0) {
                e.preventDefault();
                mostrarErro(input, 'Apenas números são aceitos');
            }
        });
    });

    // ========================================
    // 3. BLOQUEIO DE LETRAS EM CAMPOS NUMÉRICOS
    // Aplica em inputs com data-validate="number" (aceita dígitos, vírgula, ponto)
    // ========================================
    document.querySelectorAll('input[data-validate="number"]').forEach(function (input) {
        input.addEventListener('keypress', function (e) {
            var char = String.fromCharCode(e.which || e.keyCode);
            // Aceita: dígitos, vírgula, ponto, backspace, tab
            if (!/[\d,.]/.test(char) && e.which !== 8 && e.which !== 0 && e.which !== 9) {
                e.preventDefault();
                mostrarErro(input, 'Apenas números são aceitos (use vírgula para decimal)');
            }
            // Impedir mais de uma vírgula/ponto
            if ((char === ',' || char === '.') && (input.value.includes(',') || input.value.includes('.'))) {
                e.preventDefault();
            }
        });

        input.addEventListener('paste', function (e) {
            var pastedText = (e.clipboardData || window.clipboardData).getData('text');
            if (/[a-zA-Z]/.test(pastedText)) {
                e.preventDefault();
                mostrarErro(input, 'O texto colado contém letras');
            }
        });
    });

    // ========================================
    // 4. BLOQUEIO EM CAMPOS DE NÚMERO INTEIRO
    // Aplica em inputs com data-validate="integer" (só dígitos)
    // ========================================
    document.querySelectorAll('input[data-validate="integer"]').forEach(function (input) {
        input.addEventListener('keypress', function (e) {
            var char = String.fromCharCode(e.which || e.keyCode);
            if (!/\d/.test(char) && e.which !== 8 && e.which !== 0 && e.which !== 9) {
                e.preventDefault();
                mostrarErro(input, 'Apenas números inteiros são aceitos');
            }
        });

        input.addEventListener('paste', function (e) {
            var pastedText = (e.clipboardData || window.clipboardData).getData('text');
            if (/\D/.test(pastedText)) {
                e.preventDefault();
                mostrarErro(input, 'O texto colado contém caracteres inválidos');
            }
        });
    });

    // ========================================
    // 5. BLOQUEIO DE NÚMEROS EM CAMPOS DE TEXTO
    // Aplica em inputs com data-validate="text" (bloqueia dígitos)
    // ========================================
    document.querySelectorAll('input[data-validate="text"]').forEach(function (input) {
        input.addEventListener('keypress', function (e) {
            var char = String.fromCharCode(e.which || e.keyCode);
            if (/\d/.test(char)) {
                e.preventDefault();
                mostrarErro(input, 'Números não são aceitos neste campo');
            }
        });

        input.addEventListener('paste', function (e) {
            var pastedText = (e.clipboardData || window.clipboardData).getData('text');
            if (/\d/.test(pastedText)) {
                e.preventDefault();
                mostrarErro(input, 'O texto colado contém números');
            }
        });
    });

    // ========================================
    // 6. SCROLL SUAVE E CONFIRMAÇÃO DE EXCLUSÃO
    // ========================================
    document.querySelectorAll('a[href^="#"]').forEach(function (anchor) {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            var target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });

    console.log('🍰 Sistema de Vendas de Doces carregado com sucesso!');
});

// ========================================
// FUNÇÕES AUXILIARES
// ========================================

/**
 * Mostra mensagem de erro temporária abaixo do input
 */
function mostrarErro(input, mensagem) {
    // Remover erro existente
    var erroExistente = input.parentNode.querySelector('.field-error');
    if (erroExistente) erroExistente.remove();

    // Criar e adicionar mensagem de erro
    var span = document.createElement('span');
    span.className = 'field-error';
    span.textContent = '⚠️ ' + mensagem;
    input.parentNode.appendChild(span);

    // Destacar o input com borda vermelha
    input.classList.add('input-error');

    // Remover após 3 segundos
    setTimeout(function () {
        span.remove();
        input.classList.remove('input-error');
    }, 3000);
}
