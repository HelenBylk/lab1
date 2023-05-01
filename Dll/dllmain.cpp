#include "pch.h"
#include "mkl_df.h"
#include <iostream>

extern "C" __declspec(dllexport) void calculate(
	const double* nodes,
	const MKL_INT nodes_count,
	const bool is_uniform,
	const double* values,
	const double* boundary_conditions,
	const MKL_INT rawdata_nodes,
	const double* borders,
	double* spline_values,
	double* integral_value
) {
	DFTaskPtr task = nullptr;
	MKL_INT dimensions = 1;
	if (dfdNewTask1D(
		&task,
		nodes_count,
		nodes,
		is_uniform ? DF_UNIFORM_PARTITION : DF_NON_UNIFORM_PARTITION,
		dimensions,
		values,
		DF_NO_HINT
	) != DF_STATUS_OK) {
		return;
	}
	double* scoeff = nullptr;
	if ((scoeff = new double[DF_PP_CUBIC * (nodes_count - 1)]) == nullptr) {
		return;
	}
	if (dfdEditPPSpline1D(
		task,
		DF_PP_CUBIC,
		DF_PP_NATURAL,
		DF_BC_2ND_LEFT_DER | DF_BC_2ND_RIGHT_DER,
		boundary_conditions,
		DF_NO_IC,
		nullptr,
		scoeff,
		DF_NO_HINT
	) != DF_STATUS_OK) {
		delete[] scoeff;
		return;
	}
	if (dfdConstruct1D(
		task,
		DF_PP_SPLINE,
		DF_METHOD_STD
	) != DF_STATUS_OK) {
		delete[] scoeff;
		return;
	}
	const MKL_INT ndorder = 3;
	const MKL_INT dorder[] = { 1, 1, 1 };
	if (dfdInterpolate1D(
		task,
		DF_INTERP,
		DF_METHOD_PP,
		rawdata_nodes,
		borders,
		DF_UNIFORM_PARTITION,
		ndorder,
		dorder,
		nullptr,
		spline_values,
		DF_MATRIX_STORAGE_ROWS,
		nullptr
	) != DF_STATUS_OK) {
		delete[] scoeff;
		return;
	}
	const MKL_INT nlim = 1;
	if (dfdIntegrate1D(
		task,
		DF_METHOD_PP,
		nlim,
		&borders[0],
		DF_NO_HINT,
		&borders[1],
		DF_NO_HINT,
		nullptr,
		nullptr,
		integral_value,
		DF_NO_HINT
	) != DF_STATUS_OK) {
		delete[] scoeff;
		return;
	}
	delete[] scoeff;
	if (dfDeleteTask(&task) != DF_STATUS_OK) {
		return;
	}
}