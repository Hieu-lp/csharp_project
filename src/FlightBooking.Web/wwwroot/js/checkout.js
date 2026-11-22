(() => {
    const form = document.getElementById('checkout-form');
    const code = document.getElementById('Input_DiscountCode');
    const apply = document.getElementById('apply-discount');
    if (!form || !code || !apply) return;
    const feedback = document.getElementById('discount-feedback');
    code.addEventListener('input', () => {
        if (feedback) { feedback.hidden = false; feedback.textContent = 'Mã đã thay đổi. Áp dụng để cập nhật giá.'; }
    });
    const birth = document.getElementById('Input_Customer_DateOfBirth');
    const birthError = document.querySelector('[data-valmsg-for="Input.Customer.DateOfBirth"]');
    if (birth && birthError) {
        const validateBirth = () => {
            const message = !birth.value || birth.validity.badInput ? 'Nhập ngày sinh người đặt hợp lệ.'
                : birth.value > birth.max ? 'Người đặt phải đã qua ngày sinh nhật lần thứ 18.'
                : birth.value < birth.min ? 'Ngày sinh người đặt không hợp lệ.' : '';
            birth.setCustomValidity(message);
            birthError.textContent = message;
        };
        birth.addEventListener('change', validateBirth);
        birth.addEventListener('invalid', validateBirth);
    }
    let applying = false;
    code.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.isComposing) return;
        event.preventDefault();
        if (!event.repeat) form.requestSubmit(apply);
    });
    form.addEventListener('submit', event => {
        if (event.submitter?.value !== 'quote') return;
        if (applying) { event.preventDefault(); return; }
        applying = true;
        apply.setAttribute('aria-busy', 'true');
    });
    window.addEventListener('pageshow', () => { applying = false; apply.removeAttribute('aria-busy'); });
})();
