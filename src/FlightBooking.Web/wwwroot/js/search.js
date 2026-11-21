(() => {
    const departure = document.getElementById('Search_DepartureAirportId');
    const arrival = document.getElementById('Search_ArrivalAirportId');
    const swap = document.getElementById('swap-airports');
    const departureDate = document.getElementById('Search_DepartureDate');
    const returnDate = document.getElementById('Search_ReturnDate');
    const field = document.getElementById('return-date-field');
    if (!departure || !arrival || !swap || !departureDate || !returnDate || !field) return;
    swap.hidden = departure.disabled;
    swap.addEventListener('click', () => {
        [departure.value, arrival.value] = [arrival.value, departure.value];
        document.getElementById('swap-feedback').textContent = 'Đã đổi chỗ điểm đi và điểm đến.';
    });
    const updateDates = () => {
        const roundTrip = document.querySelector('input[name="Search.TripType"]:checked')?.value === 'RoundTrip';
        field.hidden = !roundTrip;
        returnDate.disabled = !roundTrip;
        returnDate.required = roundTrip;
        if (!roundTrip) returnDate.value = '';
        returnDate.min = departureDate.value;
        returnDate.setCustomValidity(roundTrip && returnDate.value && returnDate.value < departureDate.value
            ? 'Ngày về không được trước ngày đi.' : '');
    };
    departureDate.addEventListener('change', updateDates);
    returnDate.addEventListener('change', updateDates);
    document.querySelectorAll('input[name="Search.TripType"]').forEach(input => input.addEventListener('change', updateDates));
    updateDates();
    const form = document.getElementById('flight-search');
    const passengers = document.getElementById('Search_Passengers');
    if (form?.dataset.selectedFlight === 'true' && passengers) {
        passengers.addEventListener('change', () => form.requestSubmit());
    }
})();
