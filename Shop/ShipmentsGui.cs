using Microsoft.AspNetCore.Mvc;

namespace ShopServices.Shop;

internal static class ShipmentsGui
{
	public static IActionResult Get()
	{
		return new ContentResult() {
			Content = 
@"<!DOCTYPE html>
<html lang='en'>

<head>
	<meta charset='UTF-8'>
	<title>ShipmentTracker</title>
	<style>
		* {
			font-family: andale mono, monospace;
		}

		.outer-table {
			border-collapse: collapse;
		}
		.outer-table td {
			border: 1px solid black;
		}
		.outer-table input, .outer-table textarea, .outer-table select, .outer-table button {
			width: 100%;
			box-sizing: border-box;
		}
		.outer-table textarea {
			resize: vertical;
		}

		.inner-table {
			width: 100%;
		}
		.inner-table td {
			border-style: none;
		}
	</style>
</head>

<body onload='loadShipments()'>
	<div id='createdShipmentArea'></div>
	<p id='loadingLabel'>Загрузка отправок...</p>
	<button id='createdShipmentLoadingButton' onclick='loadShipments()'>⟳ Обновить список отправок</button>
	<hr/>
	<div id='newShipmentArea'>
		<button id='newShipmentButton' onclick='onInitialOrderIdEnter()'>Добавить отправку</button>
		<input id='newShipmentOrderId' type='text' placeholder='N заказа' onkeyup='if (event.keyCode == 13) onInitialOrderIdEnter()'/>
	</div>

	<datalist id='shippingCompanyDatalist'>
		<option value='СДЭК'/>
		<option value='СДЭК АДРЕС'/>
		<option value='ПОЧТА'/>
		<option value='ПОЧТА 1 КЛАСС'/>
		<option value='EMS'/>
		<option value='ЭНЕРГИЯ'/>
	</datalist>
</body>

<script>
const SHIPMENTS_API_PATH = '/api/shop/storing/shipments';

const ShipmentTableState = {
	VIEWING: 0,
	EDITING: 1,
	CREATION: 2,
}

class ShipmentTable {
	constructor() {
		this.commonInputs = [];
		this.main = this.newOuterTable();
		let row0 = this.main.insertRow(-1);
		let cell00 = row0.insertCell(-1);
		let innerTable00 = this.newInnerTable(cell00);
		let cell0_00 = innerTable00.insertRow(-1).insertCell(-1);
		this.receiverName = this.newInput(cell0_00, 'Получатель', 'text');
		this.commonInputs.push(this.receiverName);
		let cell1_00 = innerTable00.insertRow(-1).insertCell(-1);
		this.receiverPhone = this.newInput(cell1_00, 'Телефон получателя', 'tel');
		this.commonInputs.push(this.receiverPhone);
		let cell01 = row0.insertCell(-1);
		this.deliveryCity = this.newInput(cell01, 'Город', 'text');
		this.commonInputs.push(this.deliveryCity);
		let cell02 = row0.insertCell(-1);
		let innerTable02 = this.newInnerTable(cell02);
		let cell0_02 = innerTable02.insertRow(-1).insertCell(-1);
		this.shippingCompany = this.newInput(cell0_02, 'ТК', 'text');
		this.shippingCompany.setAttribute('list', 'shippingCompanyDatalist');
		this.commonInputs.push(this.shippingCompany);
		let cell1_02 = innerTable02.insertRow(-1).insertCell(-1);
		this.comment0 = this.newInput(cell1_02, 'Коммент 0', 'text');
		this.commonInputs.push(this.comment0);
		let cell03 = row0.insertCell(-1);
		this.orderIds = this.newInput(cell03, 'NN заказов', 'text'); //~
		this.commonInputs.push(this.orderIds);
		let cell04 = row0.insertCell(-1);
		this.comment1 = this.newTextarea(cell04, 'Коммент 1', 'text');
		this.commonInputs.push(this.comment1);
		let cell05 = row0.insertCell(-1);
		this.comment3 = this.newTextarea(cell05, 'Коммент 3', 'text');
		this.commonInputs.push(this.comment3);
		let row1 = this.main.insertRow(-1);
		let cell10 = row1.insertCell(-1);
		let innerTable10 = this.newInnerTable(cell10);
		let cell0_10 = innerTable10.insertRow(-1).insertCell(-1);
		this.shipmentId = this.newInput(cell0_10, 'N отправки', 'text');
		this.shipmentId.disabled = true;
		let cell1_10 = innerTable10.insertRow(-1).insertCell(-1);
		this.state = this.newSelect(cell1_10, ['--Статус--', 'ОЖИДАЕТ СБОРКУ', 'НА СБОРКЕ', 'СОБРАН', 'УПАКОВАН', 'ОТПРАВЛЕН']);
		this.commonInputs.push(this.state);
		let cell11 = row1.insertCell(-1);
		let innerTable11 = this.newInnerTable(cell11);
		let cell0_11 = innerTable11.insertRow(-1).insertCell(-1);
		this.deliveryCountry = this.newInput(cell0_11, 'Страна', 'text');
		this.commonInputs.push(this.deliveryCountry);
		let cell1_11 = innerTable11.insertRow(-1).insertCell(-1);
		this.deliveryAddress = this.newInput(cell1_11, 'Адрес доставки', 'text');
		this.commonInputs.push(this.deliveryAddress);
		let cell12 = row1.insertCell(-1);
		this.trackCode = this.newInput(cell12, 'Трек-номер', 'text'); //~
		this.commonInputs.push(this.trackCode);
		let cell13 = row1.insertCell(-1);
		let innerTable13 = this.newInnerTable(cell13);
		let cell0_13 = innerTable13.insertRow(-1).insertCell(-1);
		this.customerName = this.newInput(cell0_13, 'Заказчик', 'text');
		this.commonInputs.push(this.customerName);
		let cell1_13 = innerTable13.insertRow(-1).insertCell(-1);
		this.customerPhone = this.newInput(cell1_13, 'Телефон заказчика', 'tel');
		this.commonInputs.push(this.customerPhone);
		let cell2_13 = innerTable13.insertRow(-1).insertCell(-1);
		this.customerEmail = this.newInput(cell2_13, 'e-mail заказчика', 'email');
		this.commonInputs.push(this.customerEmail);
		let cell14 = row1.insertCell(-1);
		this.comment2 = this.newTextarea(cell14, 'Коммент 2', 'text');
		this.commonInputs.push(this.comment2);
		let cell15 = row1.insertCell(-1);
		let innerTable15 = this.newInnerTable(cell15);
		let cell0_15 = innerTable15.insertRow(-1).insertCell(-1);
		this.group = this.newSelect(cell0_15, ['--Группа--', 'В РАБОТЕ', 'КРАСНАЯ', 'ЗАКАЗЫ ИЗ НАЛИЧИЯ', 'ОЖИДАНИЕ', 'ДОЛГИЙ ЗАКАЗ', 'ВЫПОЛНЕНО']);
		this.commonInputs.push(this.group);
		this.initActionRows();
		this.moyskladDataTable = this.newOuterTable();
		this.moyskladData = null;
		this.tableState = null;
		this.unchanged = null;
		this.lock = false;
	}

	initActionRows() {
		let row2 = this.main.insertRow(-1);
		let cell20 = row2.insertCell(-1);
		cell20.setAttribute('colspan', 6);
		let innerTable20 = this.newInnerTable(cell20);
		this.actionButtons = [];
		this.actionRow_new = innerTable20.insertRow(-1);
		this.saveNewButton = this.newButton(this.actionRow_new.insertCell(-1), 'Сохранить новую');
		this.actionButtons.push(this.saveNewButton);
		this.cancelCreationButton = this.newButton(this.actionRow_new.insertCell(-1), 'Отменить создание');
		this.actionButtons.push(this.cancelCreationButton);
		this.actionRow_edit = innerTable20.insertRow(-1);
		this.saveChangesButton = this.newButton(this.actionRow_edit.insertCell(-1), 'Сохранить изменения');
		this.actionButtons.push(this.saveChangesButton);
		this.cancelEditButton = this.newButton(this.actionRow_edit.insertCell(-1), 'Отменить изменения');
		this.actionButtons.push(this.cancelEditButton);
		this.actionRow_view = innerTable20.insertRow(-1);
		this.editButton = this.newButton(this.actionRow_view.insertCell(-1), 'Изменить');
		this.actionButtons.push(this.editButton);
		this.deleteButton = this.newButton(this.actionRow_view.insertCell(-1), 'Удалить');
		this.actionButtons.push(this.deleteButton);
	}

	newOuterTable() {
		let table = document.createElement('table');
		table.setAttribute('class', 'outer-table');
		return table;
	}

	newInnerTable(hostCell) {
		let table = document.createElement('table');
		table.setAttribute('class', 'inner-table');
		hostCell.appendChild(table);
		return table;
	}

	newInput(hostCell, placeholder, type) {
		let input = document.createElement('input');
		input.setAttribute('placeholder', placeholder);
		input.setAttribute('type', type);
		hostCell.appendChild(input);
		return input;
	}

	newTextarea(hostCell, placeholder, type) {
		let input = document.createElement('textarea');
		input.setAttribute('placeholder', placeholder);
		input.setAttribute('type', type);
		hostCell.appendChild(input);
		return input;
	}

	newButton(hostCell, text) {
		let button = document.createElement('button');
		button.setAttribute('type', 'button');
		button.innerHTML = text;
		hostCell.appendChild(button);
		return button;
	}

	newSelect(hostCell, options) {
		let select = document.createElement('select');
		for (let i = 0; i < options.length; i++) {
			let option = document.createElement('option');
			option.setAttribute('value', i);
			option.innerHTML = options[i];
			select.appendChild(option);
		}
		hostCell.appendChild(select);
		return select;
	}

	nestElements(host) {
		host.appendChild(this.main);
		host.appendChild(this.moyskladDataTable);
	}

	removeElements() {
		this.main.remove();
		this.moyskladDataTable.remove();
	}

	setNew() {
		if (this.tableState == ShipmentTableState.CREATION) return;
		this.tableState = ShipmentTableState.CREATION;
		this.moyskladDataTable.style.display = null;
		this.actionRow_new.style.display = null;
		this.actionRow_edit.style.display = 'none';
		this.actionRow_view.style.display = 'none';
		for (const input of this.commonInputs) {
			input.disabled = false;
		}
	}

	setEdit() {
		if (this.tableState == ShipmentTableState.EDITING) return;
		this.tableState = ShipmentTableState.EDITING;
		this.moyskladDataTable.style.display = null;
		this.actionRow_new.style.display = 'none';
		this.actionRow_edit.style.display = null;
		this.actionRow_view.style.display = 'none';
		for (const input of this.commonInputs) {
			input.disabled = false;
		}
	}

	setView() {
		if (this.tableState == ShipmentTableState.VIEWING) return;
		this.tableState = ShipmentTableState.VIEWING;
		this.moyskladDataTable.style.display = 'none';
		this.actionRow_new.style.display = 'none';
		this.actionRow_edit.style.display = 'none';
		this.actionRow_view.style.display = null;
		for (const input of this.commonInputs) {
			input.disabled = true;
		}
	}

	acquireLock() {
		if (this.lock) return false;
		this.lock = true;
		for (const actionButton of this.actionButtons) {
			actionButton.disabled = true;
		}
		return true;
	}

	releaseLock() {
		for (const actionButton of this.actionButtons) {
			actionButton.disabled = false;
		}
		this.lock = false;
	}

	updateMoyskladData(data) {
		this.moyskladData = data;
		this.fillMoyskladDataTable(data);
		this.state.selectedIndex = 1;
		this.group.selectedIndex = 1;
		if (data == null) return;
		if (data.customer != null) {
			this.receiverName.value = data.customer.name;
			this.receiverPhone.value = data.customer.phone;
			this.customerName.value = data.customer.name;
			this.customerEmail.value = data.customer.email;
			this.customerPhone.value = data.customer.phone;
			this.comment3.value = data.customer.actualAddressComment;
		}
		if (data.customerOrder != null) {
			this.comment1.value = data.customerOrder.description;
			this.shippingCompany.value = data.customerOrder.shippingCompany;
		}
	}

	fillMoyskladDataTable(data) {
		this.moyskladDataTable.innerHTML = '<tr><th>Поле</th><th>Значение</th></tr>';
		if (data == null) return;
		if (data.customer != null) {
			this.addOptTableRow(this.moyskladDataTable, 'ФИО заказчика', data.customer.name);
			this.addOptTableRow(this.moyskladDataTable, 'E-mail заказчика', data.customer.email);
			this.addOptTableRow(this.moyskladDataTable, 'Телефон заказчика', data.customer.phone);
			this.addOptTableRow(this.moyskladDataTable, 'Фактический адрес заказчика', data.customer.actualAddress);
			this.addOptTableRow(this.moyskladDataTable, 'Комментарий к адресу заказчика', data.customer.actualAddressComment);
			this.addOptTableRow(this.moyskladDataTable, 'Адрес регистрации (юр.) заказчика', data.customer.legalAddress);
		}
		if (data.customerOrder != null) {
			this.addOptTableRow(this.moyskladDataTable, 'Комментарий к заказу', data.customerOrder.description);
			this.addOptTableRow(this.moyskladDataTable, 'ТК', data.customerOrder.shippingCompany);
		}
	}

	addOptTableRow(table, label, value) {
		if (value == null) return;
		let row = table.insertRow(-1);
		row.insertCell(-1).innerHTML = label;
		row.insertCell(-1).innerHTML = value;
	}

	toJson() {
		return {
			id: this.shipmentId.value,
			group: this.group.selectedIndex,
			state: this.state.selectedIndex,
			trackCode: this.trackCode.value,
			orderIds: this.orderIds.value,
			customerName: this.customerName.value,
			customerPhone: this.customerPhone.value,
			customerEmail: this.customerEmail.value,
			receiverName: this.receiverName.value,
			receiverPhone: this.receiverPhone.value,
			deliveryCountry: this.deliveryCountry.value,
			deliveryCity: this.deliveryCity.value,
			deliveryAddress: this.deliveryAddress.value,
			shippingCompany: this.shippingCompany.value,
			comments: [this.comment0.value, this.comment1.value, this.comment2.value, this.comment3.value],
			moyskladData: this.moyskladData,
		}
	}

	fromJson(jobj) {
		this.shipmentId.value = jobj.id;
		this.group.selectedIndex = jobj.group;
		this.state.selectedIndex = jobj.state;
		this.trackCode.value = jobj.trackCode;
		this.orderIds.value = jobj.orderIds;
		this.customerName.value = jobj.customerName;
		this.customerPhone.value = jobj.customerPhone;
		this.customerEmail.value = jobj.customerEmail;
		this.receiverName.value = jobj.receiverName;
		this.receiverPhone.value = jobj.receiverPhone;
		this.deliveryCountry.value = jobj.deliveryCountry;
		this.deliveryCity.value = jobj.deliveryCity;
		this.deliveryAddress.value = jobj.deliveryAddress;
		this.shippingCompany.value = jobj.shippingCompany;
		this.comment0.value = jobj.comments[0];
		this.comment1.value = jobj.comments[1];
		this.comment2.value = jobj.comments[2];
		this.comment3.value = jobj.comments[3];
		this.moyskladData = jobj.moyskladData;
	}

	requestPost(handler) {
		let query = new URLSearchParams({partition: 'p1', newid: true}).toString();
		let url = `${SHIPMENTS_API_PATH}?${query}`;
		let body = JSON.stringify(this.toJson());
		console.log(body);
		fetch(url, {method: 'POST', body: body}).then((response) => {
			console.log('Posting new shipment:', response);
			if (response.status == 200) {
				response.text().then((shId) => {
					console.log(shId);
					this.shipmentId.value = shId;
					handler(shId);
				}).catch((error) => {
					console.log('Failed to parse new shipment posting response:', error);
					handler(null);
				});
			} else {
				handler(null);
			}
		}).catch((error) => {
			console.log('Failed to post new shipment:', error);
			handler(null);
		});
	}

	requestPut(handler) {
		let query = new URLSearchParams({partition: 'p1'}).toString();
		let url = `${SHIPMENTS_API_PATH}?${query}`;
		let shId = this.shipmentId.value;
		let body = JSON.stringify(this.toJson());
		console.log(body);
		fetch(url, {method: 'PUT', body: body}).then((response) => {
			console.log(`Putting shipment '${shId}':`, response);
			handler((response.status >= 200) && (response.status < 300));
		}).catch((error) => {
			console.log(`Failed to put shipment '${shId}':`, error);
			handler(false);
		});
	}

	requestDelete(handler) {
		let shId = this.shipmentId.value;
		let query = new URLSearchParams({partition: 'p1', id: shId}).toString();
		let url = `${SHIPMENTS_API_PATH}?${query}`;
		fetch(url, {method: 'DELETE'}).then((response) => {
			console.log(`Deleting shipment '${shId}':`, response);
			handler((response.status >= 200) && (response.status < 300));
		}).catch((error) => {
			console.log(`Failed to delete shipment '${shId}':`, error);
			handler(false);
		});
	}

	static requestGetAll(handler) {
		let query = new URLSearchParams({partition: 'p1', all: true}).toString();
		let url = `${SHIPMENTS_API_PATH}?${query}`;
		fetch(url).then((response) => {
			console.log(`Getting shipments:`, response);
			if (response.status == 200) {
				response.json().then((shipmentPage) => {
					console.log(shipmentPage);
					handler(shipmentPage);
				}).catch((error) => {
					console.log('Failed to parse new shipment posting response:', error);
					handler(null);
				});
			} else {
				handler(null);
			}
		}).catch((error) => {
			console.log(`Failed to get shipments:`, error);
			handler(null);
		});
	}
}

var createdShipmentTables = {};

function loadShipments() {
	createdShipmentTables = {};
	createdShipmentArea.innerHTML = null;
	loadingLabel.style.display = null;
	createdShipmentLoadingButton.disabled = true;
	ShipmentTable.requestGetAll((shipmentPage) => {
		if ((shipmentPage != null) && (shipmentPage.shipments != null)) {
			for (const shipment of shipmentPage.shipments) {
				let shipmentTable = new ShipmentTable();
				shipmentTable.setView();
				linkShipmentTableActionButtons(shipmentTable);
				shipmentTable.fromJson(shipment);
				createdShipmentTables[shipmentTable.shipmentId.value] = shipmentTable;
				shipmentTable.nestElements(createdShipmentArea);
			}
		}
		loadingLabel.style.display = 'none';
		createdShipmentLoadingButton.disabled = false;
	});
}

function onInitialOrderIdEnter() {
	let orderId = newShipmentOrderId.value;
	if (orderId.length == 0) return;
	let shipmentTable = new ShipmentTable();
	shipmentTable.orderIds.value = orderId;
	newShipmentOrderId.value = null;
	shipmentTable.setNew();
	linkShipmentTableActionButtons(shipmentTable);
	shipmentTable.nestElements(createdShipmentArea);
	fetchCustomerOrder(orderId, (newData) => shipmentTable.updateMoyskladData(newData));
}

function linkShipmentTableActionButtons(shipmentTable) {
	shipmentTable.saveNewButton.onclick = () => saveNewShipmentTable(shipmentTable);
	shipmentTable.cancelCreationButton.onclick = () => cancelShipmentTableCreation(shipmentTable);
	shipmentTable.saveChangesButton.onclick = () => saveShipmentTableChanges(shipmentTable);
	shipmentTable.cancelEditButton.onclick = () => cancelShipmentTableEditing(shipmentTable);
	shipmentTable.editButton.onclick = () => editShipmentTable(shipmentTable);
	shipmentTable.deleteButton.onclick = () => deleteShipmentTable(shipmentTable);
}

function saveNewShipmentTable(table) {
	if ((table.tableState !== ShipmentTableState.CREATION) || !table.acquireLock()) return;
	table.requestPost((shId) => {
		if (shId != null) {
			createdShipmentTables[shId] = table;
			table.setView();
		}
		table.releaseLock();
	});
}

function cancelShipmentTableCreation(table) {
	if ((table.tableState !== ShipmentTableState.CREATION) || !table.acquireLock()) return;
	let shId = table.shipmentId.value;
	delete createdShipmentTables[shId];
	table.removeElements();
	table.releaseLock();
}

function saveShipmentTableChanges(table) {
	if ((table.tableState !== ShipmentTableState.EDITING) || !table.acquireLock()) return;
	table.requestPut((success) => {
		if (success) {
			table.setView();
			table.unchanged = null;
		}
		table.releaseLock();
	});
}

function cancelShipmentTableEditing(table) {
	if ((table.tableState !== ShipmentTableState.EDITING) || !table.acquireLock()) return;
	table.setView();
	if (table.unchanged != null) {
		table.fromJson(table.unchanged);
		table.unchanged = null;
	}
	table.releaseLock();
}

function editShipmentTable(table) {
	if ((table.tableState !== ShipmentTableState.VIEWING) || !table.acquireLock()) return;
	table.setEdit();
	table.unchanged = table.toJson();
	table.releaseLock();
}

function deleteShipmentTable(table) {
	if ((table.tableState !== ShipmentTableState.VIEWING) || !table.acquireLock()) return;
	table.requestDelete((success) => {
		if (success) {
			let shId = table.shipmentId.value;
			delete createdShipmentTables[shId];
			table.removeElements();
		}
		table.releaseLock();
	});
}

function fetchCustomerOrder(customerOrderId, handler) {
	let url = `/api/shop/moysklad/customer-order?code=${customerOrderId}`;
	fetch(url).then((response) => {
		console.log(`Getting customer order '${customerOrderId}':`, response);
		if (response.status != 200) {
			handler(null);
			return;
		}
		response.json().then((result) => {
			console.log(result);
			handler(result);
		}).catch((error) => {
			console.log(`Failed to parse customer order '${customerOrderId}' response:`, error);
			handler(null);
		});
	}).catch((error) => {
		console.log(`Failed to get customer order '${customerOrderId}':`, error);
		handler(null);
	});
}
</script>

</html>", ContentType = "text/html; charset=utf-8"};
	}
}
